using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Approvals.Contracts;

/// <summary>صف قائمة الاعتمادات مع أسماء الهدف مسبقًا من الاستعلام.</summary>
public sealed record InventoryApprovalProjectionRow(
    int Id,
    InventoryApprovalRequestType RequestType,
    InventoryApprovalStatus Status,
    string Reason,
    string RequestedByName,
    int RequestedByUserId,
    DateTime CreatedAtUtc,
    int? OrderId,
    string OrderNumber,
    int? StockIssueOrderId,
    long? IssueOrderNumber,
    int? IssueOrderSequenceYear,
    int? IssueOrderSequenceMonth,
    string IssueOrderWarehouseName,
    string IssueOrderOrderNumber,
    int? MovementId,
    string MovementWarehouseName,
    string MovementProductName,
    int MovementQuantity,
    InventoryDirection? MovementDirection,
    InventoryMovementType? MovementType,
    InventoryMovementStatus? MovementStatus,
    DateTime? MovementEffectiveAtUtc,
    string DisposalWarehouseName,
    string DisposalProductName,
    int? DisposalQuantity,
    int? DecidedByUserId,
    string DecidedByName,
    DateTime? DecidedAtUtc,
    string DecisionNote,
    int Version);

public sealed record ApprovalAllocationView(
    AllocationSourceKind SourceKind,
    int? WarehouseId,
    string WarehouseName,
    string SupplierName,
    int Qty);

public sealed record ApprovalItemView(
    int ProductId,
    string ProductName,
    int Quantity,
    IReadOnlyList<ApprovalAllocationView> Allocations);

public sealed record ApprovalDetailsResult(
    InventoryApprovalProjectionRow Row,
    IReadOnlyList<ApprovalItemView> ProposedItems,
    IReadOnlyList<ApprovalItemView> CurrentItems);
