using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Warehouses;

[RequirePermission(PermissionFeature.Warehouses, PermissionLevel.Read)]
public sealed record GetWarehouseQuery(int Id) : IRequest<WarehouseEdit>;
