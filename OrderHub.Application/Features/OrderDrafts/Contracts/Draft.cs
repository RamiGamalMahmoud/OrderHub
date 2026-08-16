using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public record Draft(
    Guid Id,
    DateTime LastModified,
    OrderDraftData Data);

public sealed record OrderDraftData(
    int? ClientId,
    string ClientName,
    int? PaymentMethodId,
    DeliveryMethod? DeliveryMethod,
    int? DeliverymanId,
    int? ShippingCarrierId,
    IReadOnlyList<OrderDraftItem> Items,
    IReadOnlyList<OrderDraftDeliveryStep> DeliverySteps);

public sealed record OrderDraftItem(
    int ProductId,
    string ProductName,
    string CategoryName,
    decimal Price,
    int Quantity,
    int? SupplierId,
    string SupplierName,
    IReadOnlyList<OrderDraftProperty> Properties);

public sealed record OrderDraftProperty(
    int PropertyId,
    string Value);

public sealed record OrderDraftDeliveryStep(
    int StepOrder,
    DeliveryMethod DeliveryMethod,
    int HandlerId);