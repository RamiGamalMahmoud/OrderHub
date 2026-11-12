using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Warehouses;

[RequirePermission(PermissionFeature.Warehouses, PermissionLevel.Write)]
public sealed record UpdateWarehouseCommand(WarehouseUpdateInput Input) : IRequest<Result>;
