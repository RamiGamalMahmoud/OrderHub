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

    public record OrderDeliveryStepEditDto(
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

    public record OrderItemEditDto(
        int ProductId,
        string ProductName,
        string CategoryName,
        decimal UnitPrice,
        int Quantity,
        int? SupplierId,
        string SupplierName,
        IReadOnlyCollection<CommonDtos.SupplierInfoDto> Suppliers);

    public record OrderEditDto(
        int Id,
        int ClientId,
        DeliveryMethod DeliveryMethod,
        int? DeliveryManId,
        int? ShippingCarrierId,
        int? PaymentMothodId,
        IReadOnlyCollection<OrderItemEditDto> OrderItems,
        IReadOnlyCollection<OrderDeliveryStepEditDto> DeliverySteps);

    public record OrderCreateDto(
        int ClientId,
        int OrderStatusId,
        DeliveryMethod DeliveryMethod,
        int? DeliveryManId,
        int? ShippingCarrierId,
        IEnumerable<OrderItemDtos.OrderItemDto> OrderItems,
        IEnumerable<OrderDeliveryStepCreateDto> DeliverySteps,
        int? PaymentMothodId);

    public record OrderUpdateDto(
        int Id,
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
