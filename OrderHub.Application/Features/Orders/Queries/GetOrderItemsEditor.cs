using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.Queries;

public static class GetOrderItemsEditor
{
    public record Query(IReadOnlyCollection<int> ProductIds)
        : IRequest<IReadOnlyList<OrderItem>>;

    public record OrderItem(
        int ProductId,
        decimal Price,
        string ProductName,
        string CategoryName,
        IReadOnlyList<OrderItemSupplier> Suppliers,
        IReadOnlyList<OrderItemProperty> Properties);

    public record OrderItemSupplier(
        int Id,
        string Name);

    public record OrderItemProperty(
        int Id,
        string Name,
        bool IsRequired,
        PropertyType PropertyType,
        IReadOnlyList<OrderItemPropertyOption> Options);

    public record OrderItemPropertyOption(
        int Id,
        string Name);
}