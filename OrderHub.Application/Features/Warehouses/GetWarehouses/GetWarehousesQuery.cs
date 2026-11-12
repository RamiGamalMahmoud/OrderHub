using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Warehouses;

[RequirePermission(PermissionFeature.Warehouses, PermissionLevel.Read)]
public sealed record GetWarehousesQuery : IRequest<IReadOnlyList<WarehouseListItem>>;
