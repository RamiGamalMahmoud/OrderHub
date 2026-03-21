using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderHub.Application.Commands;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
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
    private (AppDbContext Context, DbContextOptions<AppDbContext> Options) CreateInMemoryDb()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, options);
    }

    [Fact]
    public async Task Handle_Should_Add_OutboxMessages_For_All_Recipients()
    {
        // Arrange
        var (dbContext, options) = CreateInMemoryDb();
        using (dbContext)
        {

            var client = TestData.CreateClient();
            var deliveryman = TestData.CreateDeliveryman();
            var carrier = TestData.CreateShippingCarrier();
            var supplier1 = TestData.CreateSupplier("Supplier 1");
            var supplier2 = TestData.CreateSupplier("Supplier 2");
            var category = new Category("Category 1");
            var product1 = new Product("Product A", "PROD-1", category, new Money(50));
            var product2 = new Product("Product B", "PROD-2", category, new Money(100));

            dbContext.Clients.Add(client);
            dbContext.Deliverymen.Add(deliveryman);
            dbContext.ShippingCarriers.Add(carrier);
            dbContext.Suppliers.AddRange(supplier1, supplier2);
            dbContext.Categories.Add(category);
            dbContext.Products.AddRange(product1, product2);
            await dbContext.SaveChangesAsync();

            var order = new Order(client.Id, "ORD-001")
            {
                DeliveryMethod = DeliveryMethod.DeliveryChain
            };

            order.AddDeliveryStep(new OrderDeliveryStep
            {
                StepOrder = 1,
                DeliveryMethod = DeliveryMethod.DeliveryMan,
                DeliverymanId = deliveryman.Id,
                Deliveryman = deliveryman
            });
            order.AddDeliveryStep(new OrderDeliveryStep
            {
                StepOrder = 2,
                DeliveryMethod = DeliveryMethod.ShippingCompany,
                ShippingCarrierId = carrier.Id,
                ShippingCarrier = carrier
            });

            order.AddOrderItem(TestData.CreateOrderItem(product1.Id, product1.Name.Value, order.Id, 50, 2, supplier1));
            order.AddOrderItem(TestData.CreateOrderItem(product2.Id, product2.Name.Value, order.Id, 100, 1, supplier2));

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            AppDbContextFactory factory = new AppDbContextFactory(options);
            Mock<IMessenger> mock = new Mock<IMessenger>();
            BroadcastOrderStatusCommandHandler handler = new BroadcastOrderStatusCommandHandler(factory, mock.Object);

            BroadcastOrderStatusCommand command = new BroadcastOrderStatusCommand(order.Id);

            // Act
            Result result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            var outboxMessages = dbContext.OutboxMessages.ToList();
            Assert.Equal(5, outboxMessages.Count);

            Assert.Contains(outboxMessages, m => m.RecipientType == OrderHub.Domain.Enums.RecipientType.Client);
            Assert.Contains(outboxMessages, m => m.RecipientType == OrderHub.Domain.Enums.RecipientType.Deliveryman);
            Assert.Contains(outboxMessages, m => m.RecipientType == OrderHub.Domain.Enums.RecipientType.ShippingCarrier);
            Assert.Equal(2, outboxMessages.Count(m => m.RecipientType == OrderHub.Domain.Enums.RecipientType.Supplier));
        }
    }
}
