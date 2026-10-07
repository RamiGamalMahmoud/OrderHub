using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests.Infrastructure;

public class OrderLifecyclePersistenceTests
{
    [LifecyclePostgresFact]
    public async Task Schema_migrates_from_empty_database_and_requires_reasons_for_new_facts()
    {
        using var fixture = new OrderLifecyclePostgresDatabase(createSchema: false);
        using var db = fixture.CreateDbContext();
        await db.Database.MigrateAsync();
        await Seed(db);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync("""
            INSERT INTO order_status_history
                (order_id, previous_status, new_status, changed_at_utc, changed_by_user_id, changed_by_user_name, source, order_version)
            SELECT id, 'Processing', 'Cancelled', now(), (SELECT id FROM users LIMIT 1), 'Operator', 'ManualAction', version
            FROM orders
            """));
        Assert.Single(await db.OrderStatusHistories.ToListAsync());
    }

    internal static async Task<(int OrderId, int UserId)> Seed(AppDbContext db)
    {
        var city = new City("City");
        var client = new Client("Client", "Street", city, "01012345678", "+20");
        var courier = new Deliveryman("Courier", city, "01012345678");
        var user = User.Create("operator", "test-hash", "Operator");
        db.AddRange(client, courier, user);
        await db.SaveChangesAsync();
        var order = new Order(client.Id, "LIFECYCLE");
        order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
        order.AddDeliveryStep(new() { StepOrder = 10, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = courier.Id });
        order.AddDeliveryStep(new() { StepOrder = 20, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = courier.Id });
        order.InitializeHistory(new(user.Id, user.DisplayName, order.CreatedAt));
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return (order.Id, user.Id);
    }

    [LifecyclePostgresFact]
    public async Task State_history_and_step_only_version_persist_atomically_and_conflict_rolls_back_history()
    {
        using var fixture = new OrderLifecyclePostgresDatabase();
        int orderId, userId;
        using (var seed = fixture.CreateDbContext()) (orderId, userId) = await Seed(seed);
        using (var start = fixture.CreateDbContext())
        {
            var root = await start.Orders.Include(order => order.DeliverySteps).SingleAsync();
            Assert.True(root.TransitionTo(OrderStatus.OutForDelivery, new(userId, "Operator", DateTime.UtcNow)).IsSuccess);
            await start.SaveChangesAsync();
        }
        using var first = fixture.CreateDbContext();
        using var stale = fixture.CreateDbContext();
        var a = await first.Orders.Include(order => order.DeliverySteps).SingleAsync();
        var b = await stale.Orders.Include(order => order.DeliverySteps).SingleAsync();
        var stepId = a.DeliverySteps.OrderBy(step => step.StepOrder).First().Id;
        var time = DateTime.UtcNow;
        Assert.True(a.TransitionStep(stepId, DeliveryStatus.InProgress, new(userId, "Operator", time)).IsSuccess);
        await new UnitOfWork(first).SaveChangesAsync();
        Assert.True(b.TransitionTo(OrderStatus.Cancelled, new(userId, "Operator", time), "Cancel").IsSuccess);
        await Assert.ThrowsAsync<PersistenceConflictException>(() => new UnitOfWork(stale).SaveChangesAsync());
        using var verify = fixture.CreateDbContext();
        var current = await verify.Orders.SingleAsync(order => order.Id == orderId);
        Assert.Equal(OrderStatus.OutForDelivery, current.OrderStatus);
        Assert.Equal(3, current.Version);
        Assert.Equal(2, await verify.OrderStatusHistories.CountAsync());
        var started = Assert.Single(await verify.DeliveryStepStatusHistories.ToListAsync());
        Assert.Equal(DeliveryStatus.InProgress, started.NewStatus);
        Assert.Equal(DateTimeKind.Utc, started.ChangedAtUtc.Kind);
        Assert.Equal(userId, started.ChangedByUserId);
    }

    [LifecyclePostgresFact]
    public async Task History_is_immutable_and_references_restrict_deletion()
    {
        using var fixture = new OrderLifecyclePostgresDatabase();
        using (var seed = fixture.CreateDbContext()) await Seed(seed);
        using (var edit = fixture.CreateDbContext())
        {
            var history = await edit.OrderStatusHistories.SingleAsync();
            edit.Entry(history).Property(record => record.Reason).CurrentValue = "Rewrite";
            await Assert.ThrowsAsync<InvalidOperationException>(() => edit.SaveChangesAsync());
        }
        using (var delete = fixture.CreateDbContext())
        {
            delete.Orders.Remove(await delete.Orders.SingleAsync());
            await Assert.ThrowsAsync<DbUpdateException>(() => delete.SaveChangesAsync());
        }
        using var verify = fixture.CreateDbContext();
        Assert.Single(await verify.Orders.ToListAsync());
        Assert.Single(await verify.OrderStatusHistories.ToListAsync());
    }
}
