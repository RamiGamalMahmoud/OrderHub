using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Rebuild.GetRebuildHistory;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
[RequireHigherManager]
public sealed record GetRebuildHistoryQuery(int Take = 10)
    : IRequest<Result<IReadOnlyList<RebuildRunRow>>>;
