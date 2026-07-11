using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.OrderItemDtos;

namespace OrderHub.Infrastructure.Orders;

internal class OrderWriteService
{
    public Order Create(OrderCreateDto createDto, string orderNumber)
    {
        Order order = new Order(createDto.ClientId, orderNumber);
        ApplyHeader(order, createDto.ClientId, createDto.DeliveryMethod, createDto.DeliveryManId, createDto.ShippingCarrierId, createDto.PaymentMothodId);
        PopulateDetails(order, createDto.OrderItems, createDto.DeliverySteps);
        return order;
    }

    public void Update(AppDbContext appDbContext, Order order, OrderUpdateDto updateDto)
    {
        ApplyHeader(order, updateDto.ClientId, updateDto.DeliveryMethod, updateDto.DeliveryManId, updateDto.ShippingCarrierId, updateDto.PaymentMothodId);
        ResetDetails(appDbContext, order);
        PopulateDetails(order, updateDto.OrderItems, updateDto.DeliverySteps);
    }

    private static void ApplyHeader(
        Order order,
        int clientId,
        DeliveryMethod deliveryMethod,
        int? deliverymanId,
        int? shippingCarrierId,
        int? paymentMethodId)
    {
        order.ChangeClient(clientId);
        order.DeliveryMethod = deliveryMethod;
        order.DeliverymanId = deliverymanId;
        order.ShippingCarrierId = shippingCarrierId;
        order.PaymentMethodId = paymentMethodId;
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
        IEnumerable<OrderItemDto> orderItems,
        IEnumerable<OrderDeliveryStepCreateDto> deliverySteps)
    {
        foreach (OrderItemDto orderItemDto in orderItems ?? Enumerable.Empty<OrderItemDto>())
        {
            order.AddOrderItem(BuildOrderItem(orderItemDto, order.Id));
        }

        foreach (OrderDeliveryStepCreateDto deliveryStepDto in deliverySteps ?? Enumerable.Empty<OrderDeliveryStepCreateDto>())
        {
            order.AddDeliveryStep(BuildDeliveryStep(order.Id, deliveryStepDto));
        }
    }

    private static OrderItem BuildOrderItem(OrderItemDto orderItemDto, int orderId)
    {
        OrderItem orderItem = new OrderItem(
            orderItemDto.ProductId,
            orderItemDto.ProductName,
            orderId,
            orderItemDto.UnitPrice,
            orderItemDto.Quantity,
            orderItemDto.SupplierName,
            orderItemDto.SupplierId);

        return orderItem;
    }

    private static OrderDeliveryStep BuildDeliveryStep(int orderId, OrderDeliveryStepCreateDto deliveryStepDto)
    {
        return new OrderDeliveryStep
        {
            OrderId = orderId,
            StepOrder = deliveryStepDto.StepOrder,
            DeliveryMethod = deliveryStepDto.DeliveryMethod,
            DeliverymanId = deliveryStepDto.DeliveryMethod == DeliveryMethod.DeliveryMan
                ? deliveryStepDto.HandlerId
                : null,
            ShippingCarrierId = deliveryStepDto.DeliveryMethod == DeliveryMethod.ShippingCompany
                ? deliveryStepDto.HandlerId
                : null
        };
    }
}
