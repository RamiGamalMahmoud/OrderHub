using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Update;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests.Infrastructure;

public class PostgresOrderStepSyncTests
{
    [OrderSyncPostgresFact]
    public async Task Swaps_additions_removals_and_failed_save_preserve_identity_and_atomicity()
    {
        using var fixture = new Fixture();
        int orderId;
        OrderInput.Order input;
        using (var db = fixture.Db())
        {
            var city = new City("City");
            var client = new Client("Client", "Street", city, "01012345678", "+20");
            var courier = new Deliveryman("Courier", city, "01012345678");
            var product = new Product("Product", "P1", new Category("Category"), new Money(10));
            db.AddRange(client, courier, product);
            await db.SaveChangesAsync();
            var order = new Order(client.Id, "SYNC-PG");
            order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
            order.AddOrderItem(product.Id, "Product", 10, 1, 0, null, null, []);
            order.AddDeliveryStep(new() { StepOrder = 1, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = courier.Id });
            order.AddDeliveryStep(new() { StepOrder = 2, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = courier.Id });
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
            input = new(client.Id, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null,
                [new(product.Id, "Product", 1, 10, 0, null, null, [])],
                order.DeliverySteps.OrderBy(step => step.StepOrder)
                    .Select(step => new OrderInput.DeliveryStep(step.StepOrder, step.DeliveryMethod, courier.Id, step.Id)).ToList(), null);
        }

        var original = input.DeliverySteps.ToList();
        using var verify = fixture.Db();
        var created = await verify.OrderDeliverySteps.AsNoTracking().ToDictionaryAsync(step => step.Id, step => step.CreatedAt);
        input = input with { DeliverySteps = [original[1] with { StepOrder = 1 }, original[0] with { StepOrder = 2 }] };
        Assert.True((await fixture.Update(orderId, input)).IsSuccess);
        var swapped = await verify.OrderDeliverySteps.AsNoTracking().OrderBy(step => step.StepOrder).ToListAsync();
        Assert.Equal(original.AsEnumerable().Reverse().Select(step => step.Id), swapped.Select(step => (int?)step.Id));
        Assert.All(swapped, step => Assert.Equal(created[step.Id], step.CreatedAt));

        var invalid = input with { OrderSource = OrderSource.Salla,
            DeliverySteps = [original[0] with { HandlerId = int.MaxValue }, original[1]] };
        Assert.False((await fixture.Update(orderId, invalid)).IsSuccess);
        var afterFailure = await verify.OrderDeliverySteps.AsNoTracking().OrderBy(step => step.StepOrder).ToListAsync();
        Assert.Equal(swapped.Select(step => (step.Id, step.StepOrder, step.DeliverymanId)),
            afterFailure.Select(step => (step.Id, step.StepOrder, step.DeliverymanId)));
        var root = await verify.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(OrderSource.Online, root.OrderSource);
        Assert.Equal(OrderStatus.Processing, root.OrderStatus);

        input = input with { DeliverySteps = [original[0] with { StepOrder = 2 }, original[1] with { Id = null, StepOrder = 1 }] };
        Assert.True((await fixture.Update(orderId, input)).IsSuccess);
        var final = await verify.OrderDeliverySteps.AsNoTracking().OrderBy(step => step.StepOrder).ToListAsync();
        Assert.Equal(original[0].Id, final[1].Id);
        Assert.DoesNotContain(final, step => step.Id == original[1].Id);
        Assert.DoesNotContain(original, step => step.Id == final[0].Id);
    }

    public sealed class OrderSyncPostgresFactAttribute : FactAttribute
    {
        public OrderSyncPostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORDERHUB_ORDER_SYNC_TEST_POSTGRES")))
                Skip = "Set ORDERHUB_ORDER_SYNC_TEST_POSTGRES to a disposable PostgreSQL test server.";
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _admin = Environment.GetEnvironmentVariable("ORDERHUB_ORDER_SYNC_TEST_POSTGRES");
        private readonly string _database = "orderhub_sync_test_" + Guid.NewGuid().ToString("N");
        private readonly string _connection;

        public Fixture()
        {
            _connection = new NpgsqlConnectionStringBuilder(_admin) { Database = _database, Pooling = false }.ConnectionString;
            using var admin = new NpgsqlConnection(_admin);
            admin.Open();
            using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
            create.ExecuteNonQuery();
            try
            {
                using var db = Db();
                db.Database.EnsureCreated();
            }
            catch { Dispose(); throw; }
        }

        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connection).UseSnakeCaseNamingConvention().Options);

        public async Task<OrderHub.Domain.Common.Result> Update(int id, OrderInput.Order input)
        {
            using var db = Db();
            var order = await new OrderRepository(db).GetForUpdateAsync(id);
            return await new UpdateOrderHandler(new OrderRepository(db), new UnitOfWork(db)).Handle(new(id, input, order.Version), default);
        }

        public void Dispose()
        {
            using var admin = new NpgsqlConnection(_admin);
            admin.Open();
            using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
            drop.ExecuteNonQuery();
        }
    }
}
