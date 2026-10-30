using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.Contracts;

public static class OrderInput
{
    public record Order(
        int ClientId,
        OrderSource OrderSource,
        DeliveryMethod DeliveryMethod,
        int? DeliveryManId,
        int? ShippingCarrierId,
        IEnumerable<Item> OrderItems,
        IEnumerable<DeliveryStep> DeliverySteps,
        int? PaymentMethodId,
        decimal DeliveryCost = 0,
        OrderHub.Domain.Enums.DiscountType DiscountType = OrderHub.Domain.Enums.DiscountType.FixedAmount,
        decimal DiscountValue = 0);

    public record DeliveryStep(
        int StepOrder,
        DeliveryMethod DeliveryMethod,
        int HandlerId,
        int? Id = null);

    public record Item(
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal VatRate,
        string SupplierName,
        int? SupplierId,
        IEnumerable<Property> Properties);

    public record Property(
        int PropertyId,
        string Value);
}
