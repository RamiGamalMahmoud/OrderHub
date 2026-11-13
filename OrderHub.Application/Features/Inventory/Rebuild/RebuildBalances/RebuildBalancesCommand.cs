using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Rebuild.RebuildBalances;

/// <summary>
/// Explicit higher-manager repair: recomputes cached balances from the ledger and active
/// allocations. Never scheduled and never writes a movement.
/// </summary>
[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Approve)]
[RequireHigherManager]
public sealed record RebuildBalancesCommand(
    InventoryRebuildScope Scope,
    int? WarehouseId = null) : IRequest<Result<RebuildBalancesResult>>;
