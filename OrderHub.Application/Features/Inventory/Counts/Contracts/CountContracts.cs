using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Counts.Contracts;

public sealed record CountableProductRow(
    int ProductId,
    string ProductName,
    string ProductCode,
    bool HasOpenCount);

public sealed record CountLineRow(
    int LineId,
    int ProductId,
    string ProductName,
    string ProductCode,
    int BookQty,
    int BookDamagedQty,
    int? CountedQty,
    int? CountedDamagedQty,
    DateTime SnapshotAtUtc,
    DateTime? ReviewedAtUtc,
    LateMovementDecision? ReviewDecision,
    string ReviewNote,
    bool LateMovement)
{
    public bool IsCounted => CountedQty is not null && CountedDamagedQty is not null;

    public int? Delta => CountedQty is int counted ? counted - BookQty : null;

    public int? DamagedDelta => CountedDamagedQty is int counted ? counted - BookDamagedQty : null;
}

public sealed record CountRow(
    int Id,
    int WarehouseId,
    string WarehouseName,
    InventoryCountScope Scope,
    InventoryCountStatus Status,
    DateTime SnapshotAtUtc,
    int LineCount,
    int CountedLineCount,
    string StartedByName,
    DateTime? CompletedAtUtc,
    DateTime? DecidedAtUtc,
    string DecisionNote);

public sealed record CountOverview(
    bool IsKeeper,
    int? WarehouseId,
    string WarehouseName,
    IReadOnlyList<CountRow> Counts,
    IReadOnlyList<CountableProductRow> Products);

public sealed record CountDetails(
    int Id,
    int WarehouseId,
    string WarehouseName,
    InventoryCountScope Scope,
    InventoryCountStatus Status,
    int Version,
    DateTime SnapshotAtUtc,
    string StartedByName,
    DateTime? CompletedAtUtc,
    string DecidedByName,
    DateTime? DecidedAtUtc,
    string DecisionNote,
    bool IsKeeper,
    bool AllCounted,
    bool HasLateMovements,
    IReadOnlyList<CountLineRow> Lines);

public sealed record CountStartResult(int CountId, int LineCount);

public sealed record CountDecisionResult(int CountId, int Version, InventoryCountStatus Status);

public readonly record struct BookQuantities(int SoundQty, int DamagedQty);
