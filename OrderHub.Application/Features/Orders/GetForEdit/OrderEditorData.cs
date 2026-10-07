using MediatR;
using OrderHub.Domain.Enums;
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
        bool IsArchived = false);
