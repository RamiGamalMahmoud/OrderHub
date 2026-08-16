using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.Queries;

public static class GetOrderEdit
{
    public record Query(int OrderId) : IRequest<Order>;

    public record Order(
        int Id,
        int ClientId,
        DeliveryMethod DeliveryMethod,
        int? DeliveryManId,
        int? ShippingCarrierId,
        int? PaymentMothodId,
        IEnumerable<OrderItem> OrderItems,
        IEnumerable<DeliveryStep> DeliverySteps);

    public record OrderItem(
        int ProductId,
        string ProductName,
        string CategoryName,
        decimal UnitPrice,
        int Quantity,
        int? SupplierId,
        string SupplierName,
        IEnumerable<Supplier> Suppliers,
        IEnumerable<Property> Properties);

    public record DeliveryStep(
        int StepOrder,
        DeliveryMethod DeliveryMethod,
        int HandlerId);

    public record Supplier(int Id, string Name);

    public record Property(
        int PropertyId, 
        string Name,
        bool IsRequired,
        PropertyType PropertyType,
        IEnumerable<Option> Options,
        string SelectedValue = null);
    public record Option(int OptionId, string Value);
}


