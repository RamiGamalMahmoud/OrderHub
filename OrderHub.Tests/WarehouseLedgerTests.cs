using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Features.Inventory.IssueOrders.CreateStockIssueOrder;
using OrderHub.Application.Features.Inventory.IssueOrders.RecordIssueBatch;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Features.Inventory.WarehouseLedger;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class WarehouseLedgerTests
{
    [Fact]
    public async Task Ledger_returns_the_receipt_batch_and_performer_details()
    {
        using var db = new OrderAllocationTestDb();
        DateTime now = db.Clock.GetUtcNow().UtcDateTime;
        db.StockIn(db.MainWarehouse, db.Product, 4);

        var receipt = StockReceipt.Create(
            db.MainWarehouse.Id,
            StockReceiptType.StockIn,
            db.Supplier.Id,
            "فاتورة 88",
            db.Keeper.Id,
            [new StockReceiptLineData(db.Product.Id, 4)],
            now);
        db.Context.StockReceipts.Add(receipt);
        db.Context.SaveChanges();
        db.Context.InventoryMovements.Add(InventoryMovement.Create(
            db.MainWarehouse.Id,
            db.Product.Id,
            4,
            InventoryDirection.In,
            InventoryMovementType.StockIn,
            now,
            db.Keeper.Id,
            note: "توريد أول",
            sourceStockReceiptId: receipt.Id,
            nowUtc: now));
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var result = await LedgerHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseLedgerQuery(db.MainWarehouse.Id, db.Product.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(db.MainWarehouse.Name.Value, result.Value.WarehouseName);
        Assert.Equal(4, result.Value.Product.StockOnHand);
        Assert.Equal(1, result.Value.TotalCount);
        var row = Assert.Single(result.Value.Rows);
        Assert.Equal(InventoryMovementType.StockIn, row.MovementType);
        Assert.Equal(InventoryDirection.In, row.Direction);
        Assert.Equal(InventoryMovementStatus.Active, row.Status);
        Assert.Equal(4, row.Quantity);
        Assert.Equal("فاتورة 88", row.ReceiptReferenceNote);
        Assert.Equal("توريد أول", row.Note);
        Assert.Equal(db.Keeper.DisplayName, row.PerformedByName);
        Assert.Null(row.IssueOrderNumber);
        Assert.Null(row.ReversalOfMovementId);
    }

    [Fact]
    public async Task Ledger_resolves_the_issue_order_reference_of_a_batch()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);

        var created = await new CreateStockIssueOrderCommandHandler(
                db.Orders, db.Warehouses, db.IssueOrders, db.Inventory, db.UnitOfWork,
                new TestCurrentUser(db.Sales.Id), db.Clock)
            .Handle(new CreateStockIssueOrderCommand(new StockIssueOrderInput(
                orderId,
                db.MainWarehouse.Id,
                [new StockIssueOrderLineInput(allocationId, 3)])), default);
        Assert.True(created.IsSuccess);

        db.Context.ChangeTracker.Clear();
        int lineId = db.Context.StockIssueLines.AsNoTracking()
            .Single(line => line.IssueOrderId == created.Value && line.AllocationId == allocationId)
            .Id;

        var batch = await new RecordIssueBatchCommandHandler(
                db.IssueOrders, db.Inventory, db.UnitOfWork,
                new TestCurrentUser(db.Keeper.Id), db.Clock)
            .Handle(new RecordIssueBatchCommand(
                created.Value,
                false,
                default,
                [new IssueBatchLineInput(lineId, 2, null)]), default);
        Assert.True(batch.IsSuccess);

        db.Context.ChangeTracker.Clear();
        StockIssueOrder issueOrder = db.Context.StockIssueOrders.AsNoTracking()
            .Single(order => order.Id == created.Value);

        var result = await LedgerHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseLedgerQuery(db.MainWarehouse.Id, db.Product.Id), default);

        Assert.True(result.IsSuccess);
        var issueRow = Assert.Single(result.Value.Rows, row => row.MovementType == InventoryMovementType.IssueOut);
        Assert.Equal(2, issueRow.Quantity);
        Assert.Equal(InventoryDirection.Out, issueRow.Direction);
        Assert.Equal(issueOrder.WorkRecordNumber, issueRow.IssueOrderNumber);
        Assert.Equal(issueOrder.SequenceYear, issueRow.IssueOrderYear);
        Assert.Equal(issueOrder.SequenceMonth, issueRow.IssueOrderMonth);
        Assert.Equal(db.Keeper.DisplayName, issueRow.PerformedByName);
    }

    [Fact]
    public async Task Ledger_is_paged_newest_first_and_scoped()
    {
        using var db = new OrderAllocationTestDb();
        DateTime now = db.Clock.GetUtcNow().UtcDateTime;
        db.StockIn(db.MainWarehouse, db.Product, 10);

        for (int index = 0; index < 3; index++)
        {
            db.Context.InventoryMovements.Add(InventoryMovement.Create(
                db.MainWarehouse.Id,
                db.Product.Id,
                1,
                InventoryDirection.In,
                InventoryMovementType.Adjustment,
                now.AddMinutes(-index),
                db.Keeper.Id,
                note: $"تسوية {index}",
                nowUtc: now));
        }
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var firstPage = await LedgerHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseLedgerQuery(db.MainWarehouse.Id, db.Product.Id, Page: 1, PageSize: 2), default);

        Assert.True(firstPage.IsSuccess);
        Assert.Equal(3, firstPage.Value.TotalCount);
        Assert.Equal(2, firstPage.Value.Rows.Count);
        Assert.Equal("تسوية 0", firstPage.Value.Rows[0].Note);
        Assert.Equal("تسوية 1", firstPage.Value.Rows[1].Note);

        var secondPage = await LedgerHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseLedgerQuery(db.MainWarehouse.Id, db.Product.Id, Page: 2, PageSize: 2), default);

        Assert.True(secondPage.IsSuccess);
        Assert.Equal("تسوية 2", Assert.Single(secondPage.Value.Rows).Note);

        var clamped = await LedgerHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseLedgerQuery(db.MainWarehouse.Id, db.Product.Id, Page: 0, PageSize: 500), default);

        Assert.True(clamped.IsSuccess);
        Assert.Equal(1, clamped.Value.Page);
        Assert.Equal(StockPaging.MaxPageSize, clamped.Value.PageSize);

        var foreign = await LedgerHandler(db, db.Keeper.Id, AllowWrite()).Handle(
            new GetWarehouseLedgerQuery(db.SecondWarehouse.Id, db.Product.Id), default);
        Assert.False(foreign.IsSuccess);
        Assert.Equal("Forbidden", foreign.ErrorCode);

        var noBalance = await LedgerHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseLedgerQuery(db.MainWarehouse.Id, db.SecondProduct.Id), default);
        Assert.False(noBalance.IsSuccess);
        Assert.Equal("NotFound", noBalance.ErrorCode);

        var anonymous = await LedgerHandler(db, null, AllowApprove()).Handle(
            new GetWarehouseLedgerQuery(db.MainWarehouse.Id, db.Product.Id), default);
        Assert.False(anonymous.IsSuccess);
        Assert.Equal("Unauthorized", anonymous.ErrorCode);
    }

    [Fact]
    public void Every_movement_type_has_an_arabic_label()
    {
        foreach (InventoryMovementType type in Enum.GetValues<InventoryMovementType>())
            Assert.NotEqual(type.ToString(), InventoryPresentation.MovementTypeLabel(type));

        foreach (InventoryDirection direction in Enum.GetValues<InventoryDirection>())
            Assert.NotEqual(direction.ToString(), InventoryPresentation.MovementDirectionLabel(direction));

        foreach (InventoryMovementStatus status in Enum.GetValues<InventoryMovementStatus>())
            Assert.NotEqual(status.ToString(), InventoryPresentation.MovementStatusLabel(status));
    }

    private static GetWarehouseLedgerQueryHandler LedgerHandler(
        OrderAllocationTestDb db,
        int? userId,
        IAuthorizationService authorization) =>
        new(db.Warehouses, db.Inventory, authorization, new TestCurrentUser(userId));

    private static IAuthorizationService AllowApprove() =>
        new StubPermissions((_, level) => level is PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.Approve);

    private static IAuthorizationService AllowWrite() =>
        new StubPermissions((_, level) => level is PermissionLevel.Read or PermissionLevel.Write);
}
