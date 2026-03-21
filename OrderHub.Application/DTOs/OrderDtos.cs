using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;

namespace OrderHub.Application.DTOs;

public static class OrderDtos
{
    public record OrderDeliveryStepCreateDto(
        int StepOrder,
        DeliveryMethod DeliveryMethod,
        int HandlerId);

    public record OrderListDto(
        int Id,
        string OrderNumber,
        string ClientName,
        string ClientPhoneNumber,
        int ItemsCount,
        decimal Total,
        OrderStatus OrderStatus,
        EnumItem<OrderStatus> EnumItem,
        PaymentMethodListDto PaymentMethod,
        DateTime CreatedAt);

    public record OrderCreateDto(
        int ClientId,
        int OrderStatusId,
        DeliveryMethod DeliveryMethod,
        int? DeliveryManId,
        int? ShippingCarrierId,
        IEnumerable<OrderItemDtos.OrderItemDto> OrderItems,
        IEnumerable<OrderDeliveryStepCreateDto> DeliverySteps,
        int? PaymentMothodId);
}

public static class OrderItemDtos
{
    public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, string SupplierName, int? SupplierId);
}
