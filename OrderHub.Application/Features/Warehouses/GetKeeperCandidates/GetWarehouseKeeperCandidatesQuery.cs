using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Warehouses.GetKeeperCandidates;

[RequirePermission(PermissionFeature.Warehouses, PermissionLevel.Write)]
public sealed record GetWarehouseKeeperCandidatesQuery
    : IRequest<IReadOnlyList<WarehouseKeeperCandidate>>;
