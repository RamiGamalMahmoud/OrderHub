using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetItemsEditor;

public record OrderCatalogItem(
        int ProductId,
        decimal Price,
        string ProductName,
        string CategoryName,
        IReadOnlyList<OrderCatalogSupplier> Suppliers,
        IReadOnlyList<OrderCatalogProperty> Properties);
