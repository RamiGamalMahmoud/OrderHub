using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.Contracts;

public static class OrderDetails
{
    public record Order(
            int ClientId,
            DeliveryMethod DeliveryMethod,
            int? DeliveryManId,
            int? ShippingCarrierId,
            IEnumerable<Item> OrderItems,
            IEnumerable<DeliveryStep> DeliverySteps,
            int? PaymentMothodId);

    public record DeliveryStep(
            int StepOrder,
            DeliveryMethod DeliveryMethod,
            int HandlerId);

    public record Item(
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        string SupplierName,
        int? SupplierId,
        IEnumerable<Property> Properties);

    public record Property(
        int PropertyId,
        string Value);
}