using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Deliveries.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Deliveries.GetOrderDeliveries;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public sealed record GetOrderDeliveriesQuery(int OrderId)
    : IRequest<Result<OrderDeliveryOverview>>;
