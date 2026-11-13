using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Rebuild.Contracts;

/// <summary>Net sound/damaged quantities derived from the ledger for one warehouse/product pair.</summary>
public readonly record struct LedgerBalanceRow(
    int WarehouseId,
    int ProductId,
    int SoundQty,
    int DamagedQty);

/// <summary>Quantity still reserved by active warehouse allocations for one pair (Q9).</summary>
public readonly record struct ReservedBalanceRow(
    int WarehouseId,
    int ProductId,
    int ReservedQty);

public sealed record RebuildChangedPair(
    int WarehouseId,
    int ProductId,
    string ProductName,
    int BeforeStockOnHand,
    int BeforeReservedOnHand,
    int BeforeDamagedQty,
    int AfterStockOnHand,
    int AfterReservedOnHand,
    int AfterDamagedQty);

public sealed record RebuildBalancesResult(
    int LogId,
    int InspectedPairs,
    int ChangedPairs,
    IReadOnlyList<RebuildChangedPair> Changes);

public sealed record RebuildRunRow(
    int Id,
    DateTime PerformedAtUtc,
    string PerformedBy,
    InventoryRebuildScope Scope,
    string WarehouseName,
    int InspectedPairs,
    int ChangedPairs);

public sealed record RebuildLogLineRow(
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    string ProductCode,
    int BeforeStockOnHand,
    int BeforeReservedOnHand,
    int BeforeDamagedQty,
    int AfterStockOnHand,
    int AfterReservedOnHand,
    int AfterDamagedQty);

public sealed record RebuildLogDetails(
    int Id,
    DateTime PerformedAtUtc,
    string PerformedBy,
    InventoryRebuildScope Scope,
    string WarehouseName,
    int InspectedPairs,
    int ChangedPairs,
    IReadOnlyList<RebuildLogLineRow> Lines);
