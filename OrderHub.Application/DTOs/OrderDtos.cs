using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;

namespace OrderHub.Application.DTOs;

public static class OrderDtos
{
    public record AttributeNameDto(string Name);

    public record OrderItemAttributeDto(
        string Name,
        string Value);

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
        string ClientOrderTitle,
        string ClientName,
        string ClientPhoneNumber,
        int ItemsCount,
        decimal Total,
        OrderStatus OrderStatus,
        EnumItem<OrderStatus> EnumItem,
        PaymentMethodListDto PaymentMethod,
        DateTime CreatedAt,
        bool HasClientRecipient,
        bool HasSupplierRecipient,
        bool HasShippingCarrierRecipient,
        bool HasDeliverymanRecipient,
        bool IsClientMessageSent,
        bool IsSupplierMessageSent,
        bool IsShippingCarrierMessageSent,
        bool IsDeliverymanMessageSent);

    public record OrderStatusSummaryDto(
        OrderStatus Status,
        EnumItem<OrderStatus> EnumItem,
        int Count);

    public record OrderPaymentMethodSummaryDto(
        int? PaymentMethodId,
        string PaymentMethodName,
        int Count);

    public record OrderSummaryDto(
        IReadOnlyCollection<OrderStatusSummaryDto> StatusSummaries,
        IReadOnlyCollection<OrderPaymentMethodSummaryDto> PaymentMethodSummaries);

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
    public record OrderItemDto(
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        string SupplierName,
        int? SupplierId);
}
