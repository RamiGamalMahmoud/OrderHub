using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.WarehouseLedger;

public sealed record WarehouseLedgerProduct(
    int ProductId,
    string ProductName,
    string ProductCode,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty)
{
    public int Available => StockOnHand - ReservedOnHand;
}

public sealed record WarehouseLedgerRow(
    int Id,
    InventoryMovementType MovementType,
    InventoryDirection Direction,
    InventoryMovementStatus Status,
    int Quantity,
    DateTime EffectiveAtUtc,
    DateTime CreatedAtUtc,
    string PerformedByName,
    string Note,
    string ReceiptReferenceNote,
    long? IssueOrderNumber,
    int? IssueOrderYear,
    int? IssueOrderMonth,
    int? ReversalOfMovementId);

public sealed record WarehouseLedgerPage(
    string WarehouseName,
    WarehouseLedgerProduct Product,
    IReadOnlyList<WarehouseLedgerRow> Rows,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record WarehouseProductStockRow(
    int ProductId,
    string ProductName,
    string ProductCode,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty);

public sealed record WarehouseLedgerProjectionRow(
    int Id,
    InventoryMovementType MovementType,
    InventoryDirection Direction,
    InventoryMovementStatus Status,
    int Quantity,
    DateTime EffectiveAtUtc,
    DateTime CreatedAtUtc,
    string PerformedByName,
    string Note,
    string ReceiptReferenceNote,
    long? IssueOrderNumber,
    int? IssueOrderYear,
    int? IssueOrderMonth,
    int? ReversalOfMovementId);
