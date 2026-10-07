using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderHub.Application;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Create;
using OrderHub.Application.Features.Orders.Archive;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Application.Features.Orders.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.DependencyInjection;
using OrderHub.Infrastructure.Services;
using OrderHub.Infrastructure.Reopsitories;

namespace OrderHub.Tests.Infrastructure;

public class OrderWriteArchitectureTests
{
    [Fact]
    public async Task Repository_loads_tracked_items_properties_and_steps_for_update()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed();
        using var db = fixture.Db();
        var order = await new OrderRepository(db).GetForUpdateAsync(id);
        Assert.Equal(EntityState.Unchanged, db.Entry(order).State);
        var item = Assert.Single(order.OrderItems);
        Assert.Single(item.Properties);
        Assert.Single(order.DeliverySteps);
        Assert.True(db.Entry(order).Collection(nameof(Order.OrderItems)).IsLoaded);
        Assert.True(db.Entry(item).Collection(nameof(OrderItem.Properties)).IsLoaded);
        Assert.True(db.Entry(order).Collection(nameof(Order.DeliverySteps)).IsLoaded);
        Assert.All(db.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
    }

    [Fact]
    public void Registered_repository_and_unit_of_work_share_one_context_per_scope()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure("Host=localhost;Database=architecture_test;Username=test;Password=test");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        var repo = first.ServiceProvider.GetRequiredService<IOrderRepository>();
        var uow = first.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var context = first.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Same(context, typeof(OrderRepository).GetField("_dbContext", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(repo));
        Assert.Same(context, typeof(UnitOfWork).GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(uow));
        using var second = provider.CreateScope();
        Assert.NotSame(context, second.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_commits_once_only_when_replacement_is_valid(bool invalid)
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed();
        using var db = fixture.Db();
        var repository = new OrderRepository(db);
        var order = await repository.GetForUpdateAsync(id);
        var input = Input(order);
        if (invalid)
            input = input with { OrderItems = [input.OrderItems.Single() with { Quantity = 0 }] };
        var unit = CountingUnit(db);
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        var cancellation = new CancellationTokenSource();
        repo.Setup(r => r.GetForUpdateAsync(id, cancellation.Token)).ReturnsAsync(order);
        var result = await new UpdateOrderHandler(repo.Object, unit.Object).Handle(new(id, input, order.Version), cancellation.Token);
        Assert.Equal(!invalid, result.IsSuccess);
        unit.Verify(u => u.SaveChangesAsync(cancellation.Token), invalid ? Times.Never() : Times.Once());
        repo.Verify(r => r.GetForUpdateAsync(id, cancellation.Token), Times.Once());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_adds_and_commits_once_only_after_successful_domain_construction(bool invalid)
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed();
        using var db = fixture.Db();
        var realRepo = new OrderRepository(db);
        var input = Input(await realRepo.GetForUpdateAsync(id));
        if (invalid)
            input = input with { OrderItems = [input.OrderItems.Single() with { UnitPrice = 0 }] };
        var unit = CountingUnit(db);
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetNextOrderNumberAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
        repo.Setup(r => r.Add(It.IsAny<Order>())).Callback<Order>(realRepo.Add);
        var handler = new CreateOrderCommandHandler(repo.Object, unit.Object,
            new Mock<IQuotationRepository>(MockBehavior.Strict).Object,
            new Mock<IProformaInvoiceRepository>(MockBehavior.Strict).Object,
            OrderHub.Tests.Application.AuthorizationTests.Identity(true, 1, true),
            new Mock<IDraftRepository>(MockBehavior.Strict).Object);
        var result = await handler.Handle(new(null, input), default);
        Assert.Equal(!invalid, result.IsSuccess);
        repo.Verify(r => r.Add(It.IsAny<Order>()), invalid ? Times.Never() : Times.Once());
        unit.Verify(u => u.SaveChangesAsync(default), invalid ? Times.Never() : Times.Once());
    }

    [Fact]
    public async Task Archive_loads_only_required_graph_and_preserves_children()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed();
        fixture.EnableForeignKeys();
        using (var db = fixture.Db())
        {
            var repo = new OrderRepository(db);
            var root = await repo.GetForLifecycleAsync(id);
            Assert.Empty(root.OrderItems);
            var unit = CountingUnit(db);
            Assert.True((await new OrderLifecycleHandlers(repo, unit.Object,
                OrderHub.Tests.Application.AuthorizationTests.Identity(true, 1, true), TimeProvider.System)
                .Handle(new ArchiveOrderCommand(id, root.Version, "Duplicate"), default)).IsSuccess);
            unit.Verify(u => u.SaveChangesAsync(default), Times.Once());
        }
        using var check = fixture.Db();
        Assert.True((await check.Orders.SingleAsync()).IsArchived);
        Assert.Single(await check.OrderItems.ToListAsync());
        Assert.Single(await check.OrderItemProperties.ToListAsync());
        Assert.Single(await check.OrderDeliverySteps.ToListAsync());
        Assert.Single(await check.Products.ToListAsync());
    }

    [Fact]
    public async Task Archive_missing_order_returns_not_found_without_commit()
    {
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetForLifecycleAsync(999, default)).ReturnsAsync((Order)null);
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var result = await new OrderLifecycleHandlers(repo.Object, unit.Object,
            OrderHub.Tests.Application.AuthorizationTests.Identity(true, 1, true), TimeProvider.System)
            .Handle(new ArchiveOrderCommand(999, 1, "Duplicate"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Existing_commercial_document_remains_linked_after_archive()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed();
        using (var db = fixture.Db())
        {
            var order = await db.Orders.SingleAsync();
            var quotation = Quotation.Create("QT-1", DateTime.UtcNow, "Customer", "Phone", "Address",
                10, 0, 10, DateTime.UtcNow.AddDays(1), Guid.NewGuid());
            quotation.LinkToOrder(order);
            db.Quotations.Add(quotation);
            await db.SaveChangesAsync();
        }
        fixture.EnableForeignKeys();
        using (var db = fixture.Db())
        {
            var result = await new OrderLifecycleHandlers(new OrderRepository(db), new UnitOfWork(db),
                OrderHub.Tests.Application.AuthorizationTests.Identity(true, 1, true), TimeProvider.System)
                .Handle(new ArchiveOrderCommand(id, 1, "Duplicate"), default);
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }
        using var check = fixture.Db();
        Assert.Single(await check.Orders.ToListAsync());
        Assert.Single(await check.OrderItems.ToListAsync());
        Assert.Single(await check.OrderItemProperties.ToListAsync());
        Assert.Single(await check.OrderDeliverySteps.ToListAsync());
        Assert.Equal(id, (await check.Quotations.SingleAsync()).OrderId);
    }

    private static Mock<IUnitOfWork> CountingUnit(AppDbContext db)
    {
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var real = new UnitOfWork(db);
        unit.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) => real.SaveChangesAsync(token));
        unit.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken token) => real.BeginTransactionAsync(token));
        unit.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken token) => real.CommitTransactionAsync(token));
        unit.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken token) => real.RollbackTransactionAsync(token));
        return unit;
    }

    private static OrderInput.Order Input(Order order) => new(order.ClientId, order.OrderSource.Value,
        order.DeliveryMethod.Value, order.DeliverymanId, order.ShippingCarrierId,
        order.OrderItems.Select(i => new OrderInput.Item(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice.Value,
            i.VatRate, i.SupplierName, i.SupplierId, i.Properties.Select(p => new OrderInput.Property(p.PropertyId, p.Value)).ToList())).ToList(),
        order.DeliverySteps.Select(s => new OrderInput.DeliveryStep(s.StepOrder, s.DeliveryMethod, s.DeliverymanId.Value, s.Id)).ToList(), order.PaymentMethodId);

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Filename=:memory:;Foreign Keys=False");
        public Fixture()
        {
            _connection.Open();
            using var db = Db();
            db.Database.EnsureCreated();
        }
        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).UseSnakeCaseNamingConvention().Options);
        public void EnableForeignKeys()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys=ON";
            command.ExecuteNonQuery();
        }
        public async Task<int> Seed()
        {
            using var db = Db();
            var property = Property.Create("Field", PropertyType.Text);
            var product = new Product("Product", "P1", new Category("Category"), new Money(10));
            db.Properties.Add(property);
            db.Products.Add(product);
            db.Users.Add(User.Create("operator", "test-hash", "Operator"));
            await db.SaveChangesAsync();
            var order = new Order(5, "ORD-SEED");
            order.UpdateHeader(5, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, 41);
            order.AddOrderItem(product.Id, "Product", 10, 1, 14, null, null, [new(property.Id, "Saved")]);
            order.AddDeliveryStep(new() { StepOrder = 1, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = 51 });
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        }
        public void Dispose() => _connection.Dispose();
    }
}
