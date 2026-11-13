using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.IssueOrders.Contracts;

public sealed record StockIssueOrderLineInput(int AllocationId, int RequestedQty);

public sealed record StockIssueOrderInput(
    int OrderId,
    int WarehouseId,
    IReadOnlyList<StockIssueOrderLineInput> Lines);

public sealed record IssueBatchLineInput(int LineId, int Quantity, string ShortfallReason);

public sealed record StockIssueOrderSummary(
    int Id,
    long Number,
    int SequenceYear,
    int SequenceMonth,
    string WarehouseName,
    string OrderNumber,
    WorkRecordStatus Status,
    DateTime CreatedAtUtc,
    string RequestedByName,
    string AssignedToName,
    int LineCount,
    int RequestedTotal,
    int IssuedTotal,
    string ShortfallReason,
    string CancelReason);

public sealed record StockIssueBatchDetails(
    int Id,
    int Quantity,
    string RecordedByName,
    DateTime EffectiveAtUtc,
    string ShortfallReason);

public sealed record StockIssueOrderLineDetails(
    int Id,
    int ProductId,
    string ProductName,
    int RequestedQty,
    int IssuedQty,
    IReadOnlyList<StockIssueBatchDetails> Batches);

public sealed record StockIssueOrderDetails(
    int Id,
    long Number,
    int SequenceYear,
    int SequenceMonth,
    WorkRecordStatus Status,
    int OrderId,
    string OrderNumber,
    int WarehouseId,
    string WarehouseName,
    string RequestedByName,
    string AssignedToName,
    DateTime CreatedAtUtc,
    string CancelReason,
    string CancelRequestedByName,
    DateTime? CancelRequestedAtUtc,
    bool CanRecordBatches,
    bool CanRequestCancel,
    IReadOnlyList<StockIssueOrderLineDetails> Lines);

public sealed record StockIssueOrderList(
    bool CanCreate,
    bool CanRecordBatches,
    bool IsKeeper,
    string ScopeLabel,
    IReadOnlyList<StockIssueOrderSummary> Items);

public sealed record IssueOrderAvailableLine(
    int AllocationId,
    int ProductId,
    string ProductName,
    int AllocatedQty,
    int IssuedQty,
    int ActiveRequestedQty,
    int AvailableToRequest);

public sealed record IssueOrderWarehouseGroup(
    int WarehouseId,
    string WarehouseName,
    IReadOnlyList<IssueOrderAvailableLine> Lines);

public sealed record IssueOrderOrderOption(
    int Id,
    string OrderNumber,
    string ClientName,
    int AvailableAllocations);

public sealed record IssueOrderSelectedOrder(
    int OrderId,
    string OrderNumber,
    string ClientName,
    IReadOnlyList<IssueOrderWarehouseGroup> Warehouses);

public sealed record IssueOrderContext(
    bool CanCreate,
    IReadOnlyList<IssueOrderOrderOption> Orders,
    IssueOrderSelectedOrder SelectedOrder);
