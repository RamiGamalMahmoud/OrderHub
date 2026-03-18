using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Commands;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.CommandHandlers.Orders;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static OrderHub.Application.Commands.OrderCommands;

namespace OrderHub.Tests.Infrastructure.Services;

public class BroadcastOrderStatusCommandHandlerTests
{
    private AppDbContext CreateInMemoryDb()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Handle_Should_Add_OutboxMessages_For_All_Recipients()
    {
        // Arrange
        using var dbContext = CreateInMemoryDb();

        // Create test data
        var client = TestData.CreateClient();
        var deliveryman = TestData.CreateDeliveryman();
        var carrier = TestData.CreateShippingCarrier();
        var supplier1 = TestData.CreateSupplier("Supplier 1");
        var supplier2 = TestData.CreateSupplier("Supplier 2");

        var order = new Order(client.Id, "ORD-001")
        {
            Deliveryman = deliveryman,
            ShippingCarrier = carrier
        };

        // Add order items
        order.AddOrderItem(TestData.CreateOrderItem(1, "Product A", order.Id, 50, 2, supplier1));
        order.AddOrderItem(TestData.CreateOrderItem(2, "Product B", order.Id, 100, 1, supplier2));

        // Seed the DB
        dbContext.Clients.Add(client);
        dbContext.Deliverymen.Add(deliveryman);
        dbContext.ShippingCarriers.Add(carrier);
        dbContext.Suppliers.AddRange(supplier1, supplier2);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        TestAppDbContextFactory factory = new TestAppDbContextFactory(dbContext);
        BroadcastOrderStatusCommandHandler handler = new BroadcastOrderStatusCommandHandler(factory);

        BroadcastOrderStatusCommand command = new BroadcastOrderStatusCommand(order.Id);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var outboxMessages = dbContext.OutboxMessages.ToList();
        Assert.Equal(4, outboxMessages.Count); // Client + Deliveryman + ShippingCarrier + 2 suppliers = 4 messages (grouped by supplier)

        Assert.Contains(outboxMessages, m => m.RecipientType == OrderHub.Domain.Enums.RecipientType.Client);
        Assert.Contains(outboxMessages, m => m.RecipientType == OrderHub.Domain.Enums.RecipientType.Deliveryman);
        Assert.Contains(outboxMessages, m => m.RecipientType == OrderHub.Domain.Enums.RecipientType.ShippingCarrier);
        Assert.Contains(outboxMessages, m => m.RecipientType == OrderHub.Domain.Enums.RecipientType.Supplier);
    }

    // Simple factory to wrap in-memory DbContext
    private class TestAppDbContextFactory : AppDbContextFactory
    {
        private readonly AppDbContext _context;

        public TestAppDbContextFactory(AppDbContext context) : base(new DbContextOptions<AppDbContext>())
        {
            _context = context;
        }

        public new AppDbContext CreateDbContext() => _context;
    }
}