using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Rebuild.GetRebuildLog;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
[RequireHigherManager]
public sealed record GetRebuildLogQuery(int LogId) : IRequest<Result<RebuildLogDetails>>;
