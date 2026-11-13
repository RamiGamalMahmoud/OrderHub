using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Returns.Contracts;

public sealed record ReturnLineInput(int AllocationId, int IntactQty, int DamagedQty);

/// <summary>تخصيص قابل للإرجاع داخل طلب واحد مع الكميات المصروفة والمرتجعة والمستحقة.</summary>
public sealed record ReturnableAllocationRow(
    int AllocationId,
    int OrderItemId,
    int ProductId,
    string ProductName,
    int WarehouseId,
    string WarehouseName,
    int IssuedQty,
    int ReturnedQty,
    int PendingQty)
{
    public int RemainingReturnable => Math.Max(0, IssuedQty - ReturnedQty);

    public bool CanReturn => RemainingReturnable > 0 || PendingQty > 0;
}

public sealed record OrderReturnContext(
    int OrderId,
    string OrderNumber,
    string WarehouseName,
    IReadOnlyList<ReturnableAllocationRow> Rows);

/// <summary>صف «مرتجع مستحق» في قائمة أمين المخزن.</summary>
public sealed record PendingReturnRow(
    int AllocationId,
    int OrderId,
    string OrderNumber,
    int ProductId,
    string ProductName,
    int WarehouseId,
    string WarehouseName,
    int IssuedQty,
    int ReturnedQty,
    int PendingQty)
{
    public int RemainingReturnable => Math.Max(0, IssuedQty - ReturnedQty);
}

public sealed record ReturnHistoryLine(
    int ProductId,
    string ProductName,
    int IntactQty,
    int DamagedQty,
    int PendingSettledQty);

public sealed record ReturnHistoryRow(
    int Id,
    int OrderId,
    string OrderNumber,
    DateTime EffectiveAtUtc,
    string RecordedByName,
    string WarehouseName,
    IReadOnlyList<ReturnHistoryLine> Lines);

/// <summary>طلب لديه أصناف مصروفة من المخزن ما زال قابلاً لاستلام مرتجع منها.</summary>
public sealed record ReturnableOrderRow(
    int OrderId,
    string OrderNumber,
    int AllocationCount,
    int RemainingQty,
    int PendingQty);

/// <summary>صنف يحمل كمية تالفة داخل مخزن أمين المخزن، لعرض طلب الإتلاف.</summary>
public sealed record DamagedProductRow(
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    string ProductCode,
    int DamagedQty);

public sealed record ReturnsOverview(
    int WarehouseId,
    string WarehouseName,
    IReadOnlyList<PendingReturnRow> Pending,
    IReadOnlyList<ReturnableOrderRow> ReturnableOrders,
    IReadOnlyList<ReturnHistoryRow> History,
    IReadOnlyList<DamagedProductRow> DamagedProducts);

public sealed record ReturnResult(int ReturnId, int IntactQty, int DamagedQty, int PendingSettledQty);
