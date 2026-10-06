using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetItemsEditor;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public record GetOrderItemsEditorQuery(IReadOnlyCollection<int> ProductIds)
        : IRequest<IReadOnlyList<OrderCatalogItem>>;
