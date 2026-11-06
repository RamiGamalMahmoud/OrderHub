#nullable enable

using MediatR;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetForEdit;

public record OrderEditorData(
        int Id,
        int ClientId,
        DeliveryMethod? DeliveryMethod,
        int? DeliveryManId,
        int? ShippingCarrierId,
        int? PaymentMothodId,
        IEnumerable<OrderEditorItem> OrderItems,
        IEnumerable<OrderEditorDeliveryStep> DeliverySteps,
        OrderSource? OrderSource = null,
        int Version = 1,
        OrderStatus Status = OrderStatus.Processing,
        bool IsArchived = false,
        decimal DeliveryCost = 0,
        DiscountType DiscountType = DiscountType.FixedAmount,
        decimal DiscountValue = 0,
        decimal DiscountAmount = 0,
        string? OrderNumber = null,
        DateTime? ModifiedAt = null);
