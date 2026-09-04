using FluentAssertions;
using OrderHub.Application.Features.Orders.NotifyOrderParticipants;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure.Queries.Orders;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;
using OrderHub.Tests.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests.Application.Features.Orders.NotifyOrderParticipants;

public class NotifyOrderParticipantsCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_OutboxMessages_For_All_Recipients()
    {
        // Arrange
        var dbContextFactory = TestDb.CreateFactory();

        using var dbContext = dbContextFactory.CreateDbContext();

        var client = TestData.CreateClient();
        var deliveryman = TestData.CreateDeliveryman();
        var carrier = TestData.CreateShippingCarrier();

        var supplier1 = TestData.CreateSupplier("Supplier 1");
        var supplier2 = TestData.CreateSupplier("Supplier 2");

        var supplierGroup1 = new WhatsappGroup
        {
            GroupName = "Supplier Group 1",
            GroupLink = "https://chat.whatsapp.com/supplier-group-1",
            GroupType = WhatsappGroupType.Suppliers
        };

        var supplierGroup2 = new WhatsappGroup
        {
            GroupName = "Supplier Group 2",
            GroupLink = "https://chat.whatsapp.com/supplier-group-2",
            GroupType = WhatsappGroupType.Suppliers
        };

        var deliverymenGroup = new WhatsappGroup
        {
            GroupName = "Deliverymen Group",
            GroupLink = "https://chat.whatsapp.com/deliverymen-group",
            GroupType = WhatsappGroupType.Deliverymen
        };

        var category = new Category("Category 1");

        var product1 = new Product(
            "Product A",
            "PROD-1",
            category,
            new Money(50));

        var product2 = new Product(
            "Product B",
            "PROD-2",
            category,
            new Money(100));

        supplier1.WhatsappGroup = supplierGroup1;
        supplier2.WhatsappGroup = supplierGroup2;

        deliveryman.ChangeWhatsappGroup(deliverymenGroup);

        dbContext.Clients.Add(client);
        dbContext.Deliverymen.Add(deliveryman);
        dbContext.ShippingCarriers.Add(carrier);

        dbContext.WhatsappGroups.AddRange(
            supplierGroup1,
            supplierGroup2,
            deliverymenGroup);

        dbContext.Suppliers.AddRange(
            supplier1,
            supplier2);

        dbContext.Categories.Add(category);

        dbContext.Products.AddRange(
            product1,
            product2);

        await dbContext.SaveChangesAsync();

        var order = new Order(
            client.Id,
            "ORD-001");

        order.ChangeDeliveryMethod(
            DeliveryMethod.DeliveryChain);

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

        order.AddOrderItem(
            product1.Id,
            product1.Name.Value,
            200,
            50,
            supplier1.Name.Value,
            supplier1.Id,
            []);

        order.AddOrderItem(
            product2.Id,
            product2.Name.Value,
            199,
            100,
            supplier2.Name.Value,
            supplier2.Id,
            []);

        dbContext.Orders.Add(order);

        await dbContext.SaveChangesAsync();

        // Real application dependencies
        var orderNotificationQuery =
            new OrderQueries(dbContext);

        var outboxMessageRepository =
            new OutboxMessageRepository(dbContext);

        var unitOfWork =
            new UnitOfWork(dbContext);

        var handler =
            new NotifyOrderParticipantsCommandHandler(
                orderNotificationQuery,
                outboxMessageRepository,
                unitOfWork);

        var command =
            new NotifyOrderParticipantsCommand(
                order.Id,
                null);

        // Act
        Result result =
            await handler.Handle(
                command,
                CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var outboxMessages =
            dbContext.OutboxMessages
                .ToList();

        outboxMessages.Should().HaveCount(5);

        outboxMessages.Should().ContainSingle(
            message =>
                message.RecipientType ==
                RecipientType.Client);

        outboxMessages.Should().ContainSingle(
            message =>
                message.RecipientType ==
                RecipientType.Deliveryman);

        outboxMessages.Should().ContainSingle(
            message =>
                message.RecipientType ==
                RecipientType.ShippingCarrier);

        outboxMessages.Count(
            message =>
                message.RecipientType ==
                RecipientType.Supplier)
            .Should()
            .Be(2);
    }
}