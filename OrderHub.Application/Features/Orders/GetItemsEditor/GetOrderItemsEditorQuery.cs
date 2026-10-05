using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetItemsEditor;

public record GetOrderItemsEditorQuery(IReadOnlyCollection<int> ProductIds)
        : IRequest<IReadOnlyList<OrderCatalogItem>>;
