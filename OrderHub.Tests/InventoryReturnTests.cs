using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Approvals.DecideApproval;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Application.Features.Inventory.Returns.GetReturnsOverview;
using OrderHub.Application.Features.Inventory.Returns.RecordReturn;
using OrderHub.Application.Features.Inventory.Returns.RequestDisposal;
using OrderHub.Application.Features.Inventory.Returns.SettlePendingReturn;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Reopsitories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class InventoryReturnTests
{
    [Fact]
    public async Task Record_splits_intact_and_damaged_and_updates_balances()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);

        var result = await ReturnHandler(db, db.Keeper.Id).Handle(
            new RecordReturnCommand(
                orderId,
                "  استلام جزئي  ",
                [new ReturnLineInput(allocationId, 2, 1)]),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.IntactQty);
        Assert.Equal(1, result.Value.DamagedQty);

        db.Context.ChangeTracker.Clear();
        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(9, balance.StockOnHand);
        Assert.Equal(1, balance.DamagedQty);

        var record = db.Context.InventoryReturns.AsNoTracking()
            .Include(row => row.Lines)
            .Single();
        Assert.Equal(orderId, record.OrderId);
        Assert.Equal(db.MainWarehouse.Id, record.WarehouseId);
        Assert.Equal(db.Keeper.Id, record.RecordedByUserId);
        Assert.Equal("استلام جزئي", record.Note);
        var line = Assert.Single(record.Lines);
        Assert.Equal(2, line.IntactQty);
        Assert.Equal(1, line.DamagedQty);

        var movements = db.Context.InventoryMovements.AsNoTracking()
            .Where(movement => movement.SourceReturnId == record.Id)
            .OrderBy(movement => movement.Id)
            .ToList();
        Assert.Equal(2, movements.Count);
        Assert.Equal(InventoryMovementType.ReturnIn, movements[0].MovementType);
        Assert.Equal(2, movements[0].Quantity);
        Assert.Equal(InventoryMovementType.ReturnDamagedIn, movements[1].MovementType);
        Assert.Equal(1, movements[1].Quantity);
    }

    [Fact]
    public async Task Record_caps_at_the_remaining_returnable_quantity()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);

        Assert.True((await ReturnHandler(db, db.Keeper.Id).Handle(
            new RecordReturnCommand(orderId, string.Empty, [new ReturnLineInput(allocationId, 2, 0)]),
            default)).IsSuccess);

        var over = await ReturnHandler(db, db.Keeper.Id).Handle(
            new RecordReturnCommand(orderId, string.Empty, [new ReturnLineInput(allocationId, 2, 0)]),
            default);

        Assert.False(over.IsSuccess);
        Assert.Equal("Conflict", over.ErrorCode);
        Assert.Contains("1", over.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(1, db.Context.InventoryReturns.Count());
        Assert.Equal(9, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
    }

    [Fact]
    public async Task Record_requires_the_bound_keeper()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 2);
        await IssueAllAsync(db, orderId, allocationId, quantity: 2);

        var result = await ReturnHandler(db, db.Sales.Id).Handle(
            new RecordReturnCommand(orderId, string.Empty, [new ReturnLineInput(allocationId, 1, 0)]),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.ErrorCode);
        Assert.Equal(0, db.Context.InventoryReturns.Count());
    }

    [Fact]
    public async Task Pending_return_settles_exactly_once()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);
        SeedPendingReturn(db, allocationId, 2);

        var settled = await SettleHandler(db, db.Keeper.Id).Handle(
            new SettlePendingReturnCommand(allocationId, 2, 0, "فرز المستحق"),
            default);

        Assert.True(settled.IsSuccess);
        Assert.Equal(2, settled.Value.PendingSettledQty);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(0, db.Context.OrderInventoryAllocations.AsNoTracking()
            .Single(allocation => allocation.Id == allocationId).PendingReturnQty);
        Assert.Equal(9, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        var line = db.Context.InventoryReturnLines.AsNoTracking().Single();
        Assert.Equal(2, line.IntactQty);
        Assert.Equal(2, line.PendingSettledQty);

        var again = await SettleHandler(db, db.Keeper.Id).Handle(
            new SettlePendingReturnCommand(allocationId, 1, 0, "تكرار"),
            default);

        Assert.False(again.IsSuccess);
        Assert.Equal("Conflict", again.ErrorCode);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(9, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(1, db.Context.InventoryReturns.Count());
    }

    [Fact]
    public async Task Disposal_request_validates_cap_binding_and_duplicates()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);
        await ReturnDamagedAsync(db, orderId, allocationId, quantity: 2);

        var notKeeper = await DisposalHandler(db, db.Sales.Id).Handle(
            new RequestDisposalCommand(db.MainWarehouse.Id, db.Product.Id, 1, "تالف"),
            default);
        Assert.Equal("Forbidden", notKeeper.ErrorCode);

        var over = await DisposalHandler(db, db.Keeper.Id).Handle(
            new RequestDisposalCommand(db.MainWarehouse.Id, db.Product.Id, 3, "تالف"),
            default);
        Assert.Equal("Conflict", over.ErrorCode);
        Assert.Contains("2", over.ErrorMessage);

        Assert.True((await DisposalHandler(db, db.Keeper.Id).Handle(
            new RequestDisposalCommand(db.MainWarehouse.Id, db.Product.Id, 1, "تلف أثناء النقل"),
            default)).IsSuccess);

        var duplicate = await DisposalHandler(db, db.Keeper.Id).Handle(
            new RequestDisposalCommand(db.MainWarehouse.Id, db.Product.Id, 1, "تلف آخر"),
            default);
        Assert.Equal("Conflict", duplicate.ErrorCode);
        Assert.Contains("قيد الاعتماد", duplicate.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        var approval = db.Context.InventoryApprovalRequests.AsNoTracking().Single();
        Assert.Equal(InventoryApprovalRequestType.DamagedDisposal, approval.RequestType);
        Assert.Equal(InventoryApprovalStatus.Pending, approval.Status);
        Assert.Equal(db.MainWarehouse.Id, approval.WarehouseId);
        Assert.Equal(db.Product.Id, approval.ProductId);
        Assert.Equal(1, approval.DisposalQuantity);
        Assert.Equal("تلف أثناء النقل", approval.Reason);
    }

    [Fact]
    public async Task Disposal_approval_deducts_damaged_once()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);
        await ReturnDamagedAsync(db, orderId, allocationId, quantity: 2);

        Assert.True((await DisposalHandler(db, db.Keeper.Id).Handle(
            new RequestDisposalCommand(db.MainWarehouse.Id, db.Product.Id, 1, "تلف أثناء النقل"),
            default)).IsSuccess);

        db.Context.ChangeTracker.Clear();
        int approvalId = db.Context.InventoryApprovalRequests.Single().Id;

        var own = await DecideHandler(db, db.Keeper.Id).Handle(
            new DecideApprovalCommand(approvalId, true, null, 0), default);
        Assert.Equal("Forbidden", own.ErrorCode);

        var approved = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(approvalId, true, "تم الإتلاف", 0), default);
        Assert.True(approved.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(1, db.ReloadBalance(db.MainWarehouse, db.Product).DamagedQty);
        var movement = Assert.Single(db.Context.InventoryMovements.AsNoTracking()
            .Where(item => item.MovementType == InventoryMovementType.DamagedDisposal)
            .ToList());
        Assert.Equal(1, movement.Quantity);
        Assert.Equal(InventoryDirection.Out, movement.Direction);
        Assert.Equal("تلف أثناء النقل", movement.Note);

        var twice = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(approvalId, false, "تراجع", 1), default);
        Assert.Equal("Conflict", twice.ErrorCode);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryApprovalStatus.Approved,
            db.Context.InventoryApprovalRequests.AsNoTracking().Single().Status);
        Assert.Equal(1, db.ReloadBalance(db.MainWarehouse, db.Product).DamagedQty);
    }

    [Fact]
    public async Task Disposal_stale_and_rejection_leave_balances_untouched()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);
        await ReturnDamagedAsync(db, orderId, allocationId, quantity: 2);

        Assert.True((await DisposalHandler(db, db.Keeper.Id).Handle(
            new RequestDisposalCommand(db.MainWarehouse.Id, db.Product.Id, 1, "تلف"),
            default)).IsSuccess);

        BumpBalanceVersion(db);

        db.Context.ChangeTracker.Clear();
        int staleId = db.Context.InventoryApprovalRequests.Single().Id;
        var stale = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(staleId, true, null, 0), default);

        Assert.Equal("Stale", stale.ErrorCode);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryApprovalStatus.Stale,
            db.Context.InventoryApprovalRequests.AsNoTracking().Single().Status);
        Assert.Equal(2, db.ReloadBalance(db.MainWarehouse, db.Product).DamagedQty);
        Assert.Empty(db.Context.InventoryMovements.AsNoTracking()
            .Where(item => item.MovementType == InventoryMovementType.DamagedDisposal)
            .ToList());

        Assert.True((await DisposalHandler(db, db.Keeper.Id).Handle(
            new RequestDisposalCommand(db.MainWarehouse.Id, db.Product.Id, 1, "تلف"),
            default)).IsSuccess);

        db.Context.ChangeTracker.Clear();
        int pendingId = db.Context.InventoryApprovalRequests.AsNoTracking()
            .Single(item => item.Status == InventoryApprovalStatus.Pending).Id;
        var rejected = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(pendingId, false, "بانتظار معاينة الجودة", 0), default);
        Assert.True(rejected.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryApprovalStatus.Rejected,
            db.Context.InventoryApprovalRequests.AsNoTracking().Single(item => item.Id == pendingId).Status);
        Assert.Equal(2, db.ReloadBalance(db.MainWarehouse, db.Product).DamagedQty);
    }

    [Fact]
    public async Task Overview_lists_pending_damaged_and_returnable_only_for_the_keeper()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);
        await ReturnDamagedAsync(db, orderId, allocationId, quantity: 1);
        SeedPendingReturn(db, allocationId, 1);

        var overview = await new GetReturnsOverviewQueryHandler(
            db.Warehouses, Returns(db), new TestCurrentUser(db.Keeper.Id))
            .Handle(new GetReturnsOverviewQuery(), default);

        Assert.True(overview.IsSuccess);
        Assert.Equal(db.MainWarehouse.Id, overview.Value.WarehouseId);
        var pending = Assert.Single(overview.Value.Pending);
        Assert.Equal(allocationId, pending.AllocationId);
        Assert.Equal(1, pending.PendingQty);
        var damaged = Assert.Single(overview.Value.DamagedProducts);
        Assert.Equal(db.Product.Id, damaged.ProductId);
        Assert.Equal(1, damaged.DamagedQty);
        var returnable = Assert.Single(overview.Value.ReturnableOrders);
        Assert.Equal(orderId, returnable.OrderId);
        Assert.Equal(2, returnable.RemainingQty);
        Assert.Equal(1, returnable.PendingQty);
        var history = Assert.Single(overview.Value.History);
        Assert.Equal("أمين المخزن", history.RecordedByName);
        Assert.Equal(1, history.Lines.Single().DamagedQty);

        var forbidden = await new GetReturnsOverviewQueryHandler(
            db.Warehouses, Returns(db), new TestCurrentUser(db.Sales.Id))
            .Handle(new GetReturnsOverviewQuery(), default);
        Assert.Equal("Forbidden", forbidden.ErrorCode);
    }

    [Fact]
    public async Task Record_rejects_lines_beyond_the_order_or_without_a_quantity()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 2);
        await IssueAllAsync(db, orderId, allocationId, quantity: 2);
        var other = db.NewOrder("ORD-RETURN-OTHER-" + Guid.NewGuid().ToString("N"), (db.Product, 1));

        var foreign = await ReturnHandler(db, db.Keeper.Id).Handle(
            new RecordReturnCommand(other.Id, string.Empty, [new ReturnLineInput(allocationId, 1, 0)]),
            default);
        Assert.Equal("InvalidOperation", foreign.ErrorCode);

        var empty = await ReturnHandler(db, db.Keeper.Id).Handle(
            new RecordReturnCommand(orderId, string.Empty, [new ReturnLineInput(allocationId, 0, 0)]),
            default);
        Assert.Contains("أكبر من صفر", empty.ErrorMessage);
        Assert.Equal(0, db.Context.InventoryReturns.Count());
    }

    private static async Task IssueAllAsync(
        OrderAllocationTestDb db,
        int orderId,
        int allocationId,
        int quantity)
    {
        var created = await StockIssueOrderTests.CreateHandler(db).Handle(
            StockIssueOrderTests.CreateCommand(db, orderId, allocationId, quantity), default);
        Assert.True(created.IsSuccess);

        int lineId = StockIssueOrderTests.LineId(db, created.Value, allocationId);
        var batch = await StockIssueOrderTests.BatchHandler(db, db.Keeper.Id).Handle(
            StockIssueOrderTests.BatchCommand(created.Value, lineId, quantity, final: true, reason: null),
            default);
        Assert.True(batch.IsSuccess);
        db.Context.ChangeTracker.Clear();
    }

    private static async Task ReturnDamagedAsync(
        OrderAllocationTestDb db,
        int orderId,
        int allocationId,
        int quantity)
    {
        var result = await ReturnHandler(db, db.Keeper.Id).Handle(
            new RecordReturnCommand(orderId, string.Empty, [new ReturnLineInput(allocationId, 0, quantity)]),
            default);
        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
    }

    private static void SeedPendingReturn(OrderAllocationTestDb db, int allocationId, int quantity)
    {
        var allocation = db.Context.OrderInventoryAllocations.Single(item => item.Id == allocationId);
        Assert.True(allocation.SetPendingReturn(quantity).IsSuccess);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static void BumpBalanceVersion(OrderAllocationTestDb db)
    {
        var balance = db.Context.StockBalances.Single();
        balance.ApplyIn(1);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static InventoryReturnRepository Returns(OrderAllocationTestDb db) => new(db.Context);

    private static RecordReturnCommandHandler ReturnHandler(OrderAllocationTestDb db, int userId) =>
        new(db.Orders, Returns(db), db.Inventory, db.UnitOfWork, new TestCurrentUser(userId), db.Clock);

    private static SettlePendingReturnCommandHandler SettleHandler(OrderAllocationTestDb db, int userId) =>
        new(Returns(db), db.Inventory, db.UnitOfWork, new TestCurrentUser(userId), db.Clock);

    private static RequestDisposalCommandHandler DisposalHandler(OrderAllocationTestDb db, int userId) =>
        new(db.Warehouses, db.Inventory, db.Approvals, db.UnitOfWork, new TestCurrentUser(userId), db.Clock);

    private static DecideApprovalCommandHandler DecideHandler(OrderAllocationTestDb db, int userId) =>
        new(db.Approvals, db.Orders, db.IssueOrders, db.Inventory, db.Warehouses, db.UnitOfWork,
            new TestCurrentUser(userId), db.Clock);
}
