using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Infrastructure.Orders;

internal class OrderWriteService
{

    public void Update(AppDbContext appDbContext, Order order, OrderDetails.Order orderDetials)
    {
        ApplyHeader(order, orderDetials.ClientId, orderDetials.DeliveryMethod, orderDetials.DeliveryManId, orderDetials.ShippingCarrierId, orderDetials.PaymentMothodId);
        ResetDetails(appDbContext, order);
        PopulateDetails(order, orderDetials.OrderItems, orderDetials.DeliverySteps);
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
        IEnumerable<OrderDetails.Item> orderItems,
        IEnumerable<OrderDetails.DeliveryStep> deliverySteps)
    {
        foreach (OrderDetails.Item orderItemDto in orderItems ?? Enumerable.Empty<OrderDetails.Item>())
        {
            order.AddOrderItem(
                orderItemDto.ProductId,
                orderItemDto.ProductName,
                orderItemDto.UnitPrice,
                orderItemDto.Quantity,
                orderItemDto.SupplierName,
                orderItemDto.SupplierId,
                orderItemDto.Properties.Select(property => new OrderItemPropertyData(property.PropertyId, property.Value))
                    .ToList()
                    .AsReadOnly());
        }

        foreach (OrderDetails.DeliveryStep deliveryStep in deliverySteps ?? Enumerable.Empty<OrderDetails.DeliveryStep>())
        {
            order.AddDeliveryStep(BuildDeliveryStep(order.Id, deliveryStep));
        }
    }

    private static OrderDeliveryStep BuildDeliveryStep(int orderId, OrderDetails.DeliveryStep deliveryStep)
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
