using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.WarehouseStock;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetAccessibleWarehousesQuery : IRequest<Result<AccessibleWarehousesResult>>;
