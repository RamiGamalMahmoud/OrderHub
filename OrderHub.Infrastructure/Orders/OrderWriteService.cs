using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Infrastructure.Orders;

internal class OrderWriteService
{

    public void Update(AppDbContext appDbContext, Order order, OrderInput.Order orderDetials)
    {
        ApplyHeader(
            order,
            orderDetials.ClientId,
            orderDetials.DeliveryMethod,
            orderDetials.DeliveryManId,
            orderDetials.ShippingCarrierId,
            orderDetials.PaymentMethodId);

        ResetDetails(
            appDbContext, 
            order);

        PopulateDetails(
            order,
            orderDetials.OrderItems,
            orderDetials.DeliverySteps);
        order.ChangeOrderSource(orderDetials.OrderSource);
    }

    private static void ApplyHeader(
        Order order,
        int clientId,
        DeliveryMethod deliveryMethod,
        int? deliverymanId,
        int? shippingCarrierId,
        int? paymentMethodId)
    {
        order.ChangeDeliveryMethod(deliveryMethod);
        order.ChangeClient(clientId);
        order.ChangeDeliveryman(deliverymanId);
        order.ChangeShippingCarrier(shippingCarrierId);
        order.ChangePaymentMethod(paymentMethodId);
    }

    private static void ResetDetails(AppDbContext appDbContext, Order order)
    {
        appDbContext.OrderItems.RemoveRange(order.OrderItems);
        appDbContext.OrderDeliverySteps.RemoveRange(order.DeliverySteps);

        order.ClearOrderItems();
        order.ClearDeliverySteps();
    }

    private static void PopulateDetails(
        Order order,
        IEnumerable<OrderInput.Item> orderItems,
        IEnumerable<OrderInput.DeliveryStep> deliverySteps)
    {
        foreach (OrderInput.Item item in orderItems ?? Enumerable.Empty<OrderInput.Item>())
        {
            order.AddOrderItem(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.VatRate,
                item.SupplierName,
                item.SupplierId,
                item.Properties.Select(property => new OrderItemPropertyData(
                    property.PropertyId,
                    property.Value))
                    .ToList()
                    .AsReadOnly());
        }

        foreach (OrderInput.DeliveryStep deliveryStep in deliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>())
        {
            order.AddDeliveryStep(BuildDeliveryStep(order.Id, deliveryStep));
        }
    }

    private static OrderDeliveryStep BuildDeliveryStep(int orderId, OrderInput.DeliveryStep deliveryStep)
    {
        return new OrderDeliveryStep
        {
            OrderId = orderId,
            StepOrder = deliveryStep.StepOrder,
            DeliveryMethod = deliveryStep.DeliveryMethod,
            DeliverymanId = deliveryStep.DeliveryMethod == DeliveryMethod.DeliveryMan
                ? deliveryStep.HandlerId
                : null,
            ShippingCarrierId = deliveryStep.DeliveryMethod == DeliveryMethod.ShippingCompany
                ? deliveryStep.HandlerId
                : null
        };
    }
}
