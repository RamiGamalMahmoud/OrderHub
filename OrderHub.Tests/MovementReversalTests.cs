using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Approvals.DecideApproval;
using OrderHub.Application.Features.Inventory.Approvals.RequestMovementReversal;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Features.Inventory.IssueOrders.CreateStockIssueOrder;
using OrderHub.Application.Features.Inventory.IssueOrders.RecordIssueBatch;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class MovementReversalTests
{
    [Fact]
    public async Task Keeper_request_creates_a_pending_approval_and_leaves_the_ledger_untouched()
    {
        using var db = new OrderAllocationTestDb();
        int movementId = SeedStockIn(db);

        var result = await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "تسجيل خاطئ"),
            default);

        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
        var approval = Approval(db);
        Assert.Equal(InventoryApprovalRequestType.MovementReversal, approval.RequestType);
        Assert.Equal(InventoryApprovalStatus.Pending, approval.Status);
        Assert.Equal(movementId, approval.InventoryMovementId);
        Assert.Equal(db.Keeper.Id, approval.RequestedByUserId);
        Assert.Equal(InventoryMovementStatus.Active, Movement(db, movementId).Status);
        Assert.Equal(10, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(1, db.Context.InventoryMovements.Count());
    }

    [Fact]
    public async Task Requests_are_limited_to_the_movement_keeper_or_an_approver()
    {
        using var db = new OrderAllocationTestDb();
        int movementId = SeedStockIn(db);

        var stranger = await RequestHandler(db, db.Sales.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "طلب عكس"),
            default);

        Assert.False(stranger.IsSuccess);
        Assert.Equal("Forbidden", stranger.ErrorCode);
        Assert.Empty(db.Context.InventoryApprovalRequests);

        var manager = await RequestHandler(db, db.Sales.Id, canApprove: true).Handle(
            new RequestMovementReversalCommand(movementId, "طلب عكس"),
            default);

        Assert.True(manager.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryApprovalStatus.Pending, Approval(db).Status);
    }

    [Fact]
    public async Task Duplicate_pending_requests_are_rejected()
    {
        using var db = new OrderAllocationTestDb();
        int movementId = SeedStockIn(db);
        Assert.True((await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "تسجيل خاطئ"), default)).IsSuccess);

        var duplicate = await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "مرة أخرى"),
            default);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("Conflict", duplicate.ErrorCode);
        Assert.Equal(1, db.Context.InventoryApprovalRequests.Count());
    }

    [Fact]
    public async Task Reversal_of_a_reversal_is_rejected()
    {
        using var db = new OrderAllocationTestDb();
        int movementId = SeedStockIn(db);
        Assert.True((await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "تسجيل خاطئ"), default)).IsSuccess);
        Assert.True((await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 0), default)).IsSuccess);

        db.Context.ChangeTracker.Clear();
        int reversalId = db.Context.InventoryMovements.AsNoTracking()
            .Single(movement => movement.MovementType == InventoryMovementType.Reversal)
            .Id;

        var result = await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(reversalId, "عكس العكس"),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("عكسية", result.ErrorMessage);
    }

    [Fact]
    public async Task Approved_reversal_posts_an_offsetting_movement_at_decision_time()
    {
        using var db = new OrderAllocationTestDb();
        int movementId = SeedStockIn(db);
        Assert.True((await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "تسجيل خاطئ"), default)).IsSuccess);

        var decision = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, "تم العكس", 0),
            default);

        Assert.True(decision.IsSuccess);
        db.Context.ChangeTracker.Clear();

        var original = Movement(db, movementId);
        Assert.Equal(InventoryMovementStatus.Reversed, original.Status);

        var reversal = db.Context.InventoryMovements.AsNoTracking()
            .Single(movement => movement.MovementType == InventoryMovementType.Reversal);
        Assert.Equal(original.Id, reversal.ReversalOfMovementId);
        Assert.Equal(10, reversal.Quantity);
        Assert.Equal(InventoryDirection.Out, reversal.Direction);
        Assert.Equal(db.Clock.GetUtcNow().UtcDateTime, reversal.EffectiveAtUtc);
        Assert.Equal(db.Sales.Id, reversal.CreatedByUserId);
        Assert.Equal("تسجيل خاطئ", reversal.Note);

        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(InventoryApprovalStatus.Approved, Approval(db).Status);
    }

    [Fact]
    public async Task Keeper_may_approve_his_own_reversal_request()
    {
        using var db = new OrderAllocationTestDb();
        int movementId = SeedStockIn(db);
        Assert.True((await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "تسجيل خاطئ"), default)).IsSuccess);

        var decision = await DecideHandler(db, db.Keeper.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 0),
            default);

        Assert.True(decision.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryMovementStatus.Reversed, Movement(db, movementId).Status);
        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
    }

    [Fact]
    public async Task Approved_reversal_of_an_issue_batch_restores_stock_and_the_reservation()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 5);
        var created = await CreateIssueOrderHandler(db).Handle(
            new CreateStockIssueOrderCommand(new StockIssueOrderInput(
                orderId,
                db.MainWarehouse.Id,
                [new StockIssueOrderLineInput(allocationId, 5)])),
            default);
        Assert.True(created.IsSuccess);

        db.Context.ChangeTracker.Clear();
        int lineId = db.Context.StockIssueLines.AsNoTracking()
            .Single(line => line.IssueOrderId == created.Value)
            .Id;
        Assert.True((await BatchHandler(db).Handle(
            new RecordIssueBatchCommand(created.Value, false, default, [new IssueBatchLineInput(lineId, 2, null)]),
            default)).IsSuccess);

        db.Context.ChangeTracker.Clear();
        int issueMovementId = db.Context.InventoryMovements.AsNoTracking()
            .Single(movement => movement.MovementType == InventoryMovementType.IssueOut)
            .Id;
        Assert.Equal(8, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(3, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);

        Assert.True((await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(issueMovementId, "صرف بالخطأ"), default)).IsSuccess);

        var decision = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 0),
            default);

        Assert.True(decision.IsSuccess);
        db.Context.ChangeTracker.Clear();

        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(10, balance.StockOnHand);
        Assert.Equal(5, balance.ReservedOnHand);
        Assert.Equal(0, db.Context.StockIssueBatches.Count());

        var allocation = db.Context.OrderInventoryAllocations.AsNoTracking()
            .Single(item => item.Id == allocationId);
        Assert.Equal(0, allocation.IssuedQty);
        Assert.Equal(5, allocation.Qty);

        Assert.Equal(WorkRecordStatus.Pending, db.Context.StockIssueOrders.AsNoTracking()
            .Single(order => order.Id == created.Value)
            .Status);
        Assert.Equal(InventoryMovementStatus.Reversed, Movement(db, issueMovementId).Status);
        var reversal = db.Context.InventoryMovements.AsNoTracking()
            .Single(movement => movement.MovementType == InventoryMovementType.Reversal);
        Assert.Equal(InventoryDirection.In, reversal.Direction);
        Assert.Equal(2, reversal.Quantity);
    }

    [Fact]
    public async Task Already_reversed_movement_marks_the_request_stale_without_side_effects()
    {
        using var db = new OrderAllocationTestDb();
        int movementId = SeedStockIn(db);
        Assert.True((await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "تسجيل خاطئ"), default)).IsSuccess);

        var movement = await db.Inventory.GetMovementForUpdateAsync(movementId, default);
        Assert.True(movement!.MarkReversed().IsSuccess);
        await db.UnitOfWork.SaveChangesAsync(default);
        db.Context.ChangeTracker.Clear();

        var decision = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 0),
            default);

        Assert.False(decision.IsSuccess);
        Assert.Equal("Stale", decision.ErrorCode);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryApprovalStatus.Stale, Approval(db).Status);
        Assert.Equal(1, db.Context.InventoryMovements.Count());
        Assert.Equal(10, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
    }

    [Fact]
    public async Task Decision_with_the_wrong_version_conflicts()
    {
        using var db = new OrderAllocationTestDb();
        int movementId = SeedStockIn(db);
        Assert.True((await RequestHandler(db, db.Keeper.Id, canApprove: false).Handle(
            new RequestMovementReversalCommand(movementId, "تسجيل خاطئ"), default)).IsSuccess);

        var decision = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 3),
            default);

        Assert.False(decision.IsSuccess);
        Assert.Equal("Conflict", decision.ErrorCode);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryApprovalStatus.Pending, Approval(db).Status);
        Assert.Equal(InventoryMovementStatus.Active, Movement(db, movementId).Status);
    }

    private static int SeedStockIn(OrderAllocationTestDb db)
    {
        DateTime now = db.Clock.GetUtcNow().UtcDateTime;
        var balance = StockBalance.Create(db.MainWarehouse.Id, db.Product.Id);
        balance.ApplyIn(10);
        db.Context.StockBalances.Add(balance);
        var movement = InventoryMovement.Create(
            db.MainWarehouse.Id,
            db.Product.Id,
            10,
            InventoryDirection.In,
            InventoryMovementType.StockIn,
            now,
            db.Keeper.Id,
            "توريد",
            nowUtc: now);
        db.Context.InventoryMovements.Add(movement);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        return movement.Id;
    }

    private static RequestMovementReversalCommandHandler RequestHandler(
        OrderAllocationTestDb db,
        int userId,
        bool canApprove) =>
        new(
            db.Inventory,
            db.Warehouses,
            new StubPermissions((_, level) => canApprove && level == PermissionLevel.Approve),
            db.Approvals,
            db.UnitOfWork,
            new TestCurrentUser(userId),
            db.Clock);

    private static DecideApprovalCommandHandler DecideHandler(OrderAllocationTestDb db, int userId) =>
        new(db.Approvals, db.Orders, db.IssueOrders, db.Inventory, db.Warehouses, db.UnitOfWork,
            new TestCurrentUser(userId), db.Clock);

    private static CreateStockIssueOrderCommandHandler CreateIssueOrderHandler(OrderAllocationTestDb db) =>
        new(db.Orders, db.Warehouses, db.IssueOrders, db.Inventory, db.UnitOfWork,
            new TestCurrentUser(db.Sales.Id), db.Clock);

    private static RecordIssueBatchCommandHandler BatchHandler(OrderAllocationTestDb db) =>
        new(db.IssueOrders, db.Inventory, db.UnitOfWork, new TestCurrentUser(db.Keeper.Id), db.Clock);

    private static InventoryApprovalRequest Approval(OrderAllocationTestDb db) =>
        db.Context.InventoryApprovalRequests.AsNoTracking().Single();

    private static InventoryMovement Movement(OrderAllocationTestDb db, int movementId) =>
        db.Context.InventoryMovements.AsNoTracking().Single(movement => movement.Id == movementId);
}
