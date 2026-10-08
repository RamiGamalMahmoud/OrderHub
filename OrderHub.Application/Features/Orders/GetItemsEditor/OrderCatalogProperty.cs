using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetItemsEditor;

public record OrderCatalogProperty(
        int Id,
        string Name,
        bool IsRequired,
        PropertyType PropertyType,
        IReadOnlyList<OrderCatalogPropertyOption> Options);
