using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Features.Inventory.IssueOrders.CreateStockIssueOrder;
using OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrderDetails;
using OrderHub.Application.Features.Inventory.IssueOrders.RecordIssueBatch;
using OrderHub.Application.Features.Inventory.IssueOrders.RequestIssueOrderCancel;
using OrderHub.Application.Features.Orders.Allocations;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Application.Features.Orders.Update;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class StockIssueOrderTests
{
    [Fact]
    public async Task Create_assigns_a_monthly_number_per_warehouse_and_starts_pending()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await ReserveAsync(db, quantity: 3);

        var result = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 2), default);

        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
        var created = db.Context.StockIssueOrders.AsNoTracking()
            .Include(order => order.Lines)
            .Single(order => order.Id == result.Value);
        Assert.Equal(WorkRecordStatus.Pending, created.Status);
        Assert.True(created.WorkRecordNumber > 0);
        Assert.Equal(2026, created.SequenceYear);
        Assert.Equal(11, created.SequenceMonth);
        Assert.Equal(db.MainWarehouse.Id, created.NumberingPartyId);
        Assert.Equal(db.MainWarehouse.Id, created.AssignedUserId);
        Assert.Equal(2, created.Lines.Sum(line => line.RequestedQty));

        var provider = db.Numbering;
        Assert.Equal(1, provider.Calls);
        Assert.Equal(new[] { db.MainWarehouse.Id }, provider.PartyIds);
        Assert.Equal(db.Clock.GetUtcNow(), provider.Timestamps[0]);
    }

    [Fact]
    public async Task Create_enforces_the_active_requested_cap_per_allocation()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await ReserveAsync(db, quantity: 3);

        Assert.True((await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 3), default)).IsSuccess);

        var overrun = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 1), default);

        Assert.False(overrun.IsSuccess);
        Assert.Contains("المتاح", overrun.ErrorMessage);
        Assert.Equal(1, db.Context.StockIssueOrders.Count());
    }

    [Fact]
    public async Task IssueBatch_accumulates_and_a_final_short_batch_requires_a_reason()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await ReserveAsync(db, quantity: 5);
        var created = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 5), default);
        Assert.True(created.IsSuccess);
        int issueOrderId = created.Value;
        int lineId = LineId(db, issueOrderId, allocationId);

        var first = await BatchHandler(db, db.Keeper.Id).Handle(
            BatchCommand(issueOrderId, lineId, 2, final: false, reason: null), default);

        Assert.True(first.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(WorkRecordStatus.PartiallyExecuted, Status(db, issueOrderId));
        Assert.Equal(8, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(3, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);

        var missingReason = await BatchHandler(db, db.Keeper.Id).Handle(
            BatchCommand(issueOrderId, lineId, 2, final: true, reason: null), default);

        Assert.False(missingReason.IsSuccess);
        Assert.Contains("سبب النقص", missingReason.ErrorMessage);

        var closed = await BatchHandler(db, db.Keeper.Id).Handle(
            BatchCommand(issueOrderId, lineId, 3, final: true, reason: null), default);
        Assert.True(closed.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(WorkRecordStatus.Completed, Status(db, issueOrderId));
        Assert.Equal(5, db.Context.OrderInventoryAllocations.AsNoTracking()
            .Single(allocation => allocation.Id == allocationId).IssuedQty);
        Assert.Equal(5, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Equal(2, db.Context.InventoryMovements.Count(movement => movement.MovementType == InventoryMovementType.IssueOut));
    }

    [Fact]
    public async Task IssueBatch_accepts_a_final_shortfall_with_a_reason()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await ReserveAsync(db, quantity: 4);
        var created = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 4), default);
        Assert.True(created.IsSuccess);
        int issueOrderId = created.Value;
        int lineId = LineId(db, issueOrderId, allocationId);

        var result = await BatchHandler(db, db.Keeper.Id).Handle(
            BatchCommand(issueOrderId, lineId, 3, final: true, reason: "نقص بالمخزون"), default);

        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(WorkRecordStatus.Completed, Status(db, issueOrderId));
        var batch = db.Context.StockIssueBatches.AsNoTracking().Single();
        Assert.Equal("نقص بالمخزون", batch.ShortfallReason);
        Assert.Equal(3, db.Context.OrderInventoryAllocations.AsNoTracking().Single().IssuedQty);
        Assert.Equal(7, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal(1, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
    }

    [Fact]
    public async Task IssueBatch_is_limited_to_the_bound_warehouse_keeper()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await ReserveAsync(db, quantity: 2);
        var created = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 2), default);
        Assert.True(created.IsSuccess);
        int lineId = LineId(db, created.Value, allocationId);

        var result = await BatchHandler(db, db.Sales.Id).Handle(
            BatchCommand(created.Value, lineId, 1, final: false, reason: null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.ErrorCode);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(WorkRecordStatus.Pending, Status(db, created.Value));
    }

    [Fact]
    public async Task Cancel_request_blocks_batches_and_new_orders_until_the_order_closes()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await ReserveAsync(db, quantity: 3);
        var created = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 2), default);
        Assert.True(created.IsSuccess);
        int issueOrderId = created.Value;
        int lineId = LineId(db, issueOrderId, allocationId);

        var requested = await CancelHandler(db).Handle(
            new RequestIssueOrderCancelCommand(issueOrderId, "الكمية غير متوفرة"), default);

        Assert.True(requested.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(WorkRecordStatus.CancelRequested, Status(db, issueOrderId));

        var batch = await BatchHandler(db, db.Keeper.Id).Handle(
            BatchCommand(issueOrderId, lineId, 1, final: false, reason: null), default);
        Assert.False(batch.IsSuccess);

        var another = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 1), default);
        Assert.False(another.IsSuccess);
        Assert.Equal("Conflict", another.ErrorCode);
    }

    [Fact]
    public async Task Closing_the_order_auto_cancels_active_issue_orders()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await ReserveAsync(db, quantity: 3);
        var created = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 3), default);
        Assert.True(created.IsSuccess);

        var lifecycle = new OrderLifecycleHandlers(
            db.Orders, db.UnitOfWork, db.Inventory, db.IssueOrders,
            new TestCurrentUser(db.Sales.Id), db.Clock);

        var cancelled = await lifecycle.Handle(
            new ChangeOrderStatusCommand(orderId, version, OrderStatus.Cancelled, "طلب العميل الإلغاء"),
            default);

        Assert.True(cancelled.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(WorkRecordStatus.Cancelled, Status(db, created.Value));
        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Contains(db.Context.WorkRecordStatusHistories.AsNoTracking(),
            history => history.WorkRecordType == WorkRecordType.StockIssueOrder &&
                history.WorkRecordId == created.Value &&
                history.Status == WorkRecordStatus.Cancelled);
    }

    [Fact]
    public async Task Terminal_orders_reject_batches_and_cancel_requests()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await ReserveAsync(db, quantity: 2);
        var created = await CreateHandler(db).Handle(CreateCommand(db, orderId, allocationId, 2), default);
        Assert.True(created.IsSuccess);
        int issueOrderId = created.Value;
        int lineId = LineId(db, issueOrderId, allocationId);

        Assert.True((await BatchHandler(db, db.Keeper.Id).Handle(
            BatchCommand(issueOrderId, lineId, 2, final: false, reason: null), default)).IsSuccess);

        var batch = await BatchHandler(db, db.Keeper.Id).Handle(
            BatchCommand(issueOrderId, lineId, 1, final: false, reason: null), default);
        Assert.False(batch.IsSuccess);

        var cancel = await CancelHandler(db).Handle(
            new RequestIssueOrderCancelCommand(issueOrderId, "إلغاء"), default);
        Assert.False(cancel.IsSuccess);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(WorkRecordStatus.Completed, Status(db, issueOrderId));
    }

    internal static int LineId(OrderAllocationTestDb db, int issueOrderId, int allocationId)
    {
        db.Context.ChangeTracker.Clear();
        return db.Context.StockIssueLines.AsNoTracking()
            .Single(line => line.IssueOrderId == issueOrderId && line.AllocationId == allocationId)
            .Id;
    }

    private static WorkRecordStatus Status(OrderAllocationTestDb db, int issueOrderId) =>
        db.Context.StockIssueOrders.AsNoTracking()
            .Single(order => order.Id == issueOrderId)
            .Status;

    internal static CreateStockIssueOrderCommand CreateCommand(
        OrderAllocationTestDb db, int orderId, int allocationId, int quantity) =>
        new(new StockIssueOrderInput(
            orderId,
            db.MainWarehouse.Id,
            [new StockIssueOrderLineInput(allocationId, quantity)]));

    internal static RecordIssueBatchCommand BatchCommand(
        int issueOrderId,
        int lineId,
        int quantity,
        bool final,
        string? reason) =>
        new(issueOrderId, final, default, [new IssueBatchLineInput(lineId, quantity, reason)]);

    internal static CreateStockIssueOrderCommandHandler CreateHandler(OrderAllocationTestDb db) =>
        new(db.Orders, db.Warehouses, db.IssueOrders, db.Inventory, db.UnitOfWork,
            new TestCurrentUser(db.Sales.Id), db.Clock);

    internal static RecordIssueBatchCommandHandler BatchHandler(OrderAllocationTestDb db, int userId) =>
        new(db.IssueOrders, db.Inventory, db.UnitOfWork, new TestCurrentUser(userId), db.Clock);

    private static RequestIssueOrderCancelCommandHandler CancelHandler(OrderAllocationTestDb db) =>
        new(db.IssueOrders, db.Approvals, db.UnitOfWork, new TestCurrentUser(db.Keeper.Id), db.Clock);

    internal static async Task<(int OrderId, int Version, int AllocationId)> ReserveAsync(
        OrderAllocationTestDb db,
        int quantity)
    {
        db.StockIn(db.MainWarehouse, db.Product, 10);
        var order = db.NewOrder("ORD-ISSUE-" + Guid.NewGuid().ToString("N"), (db.Product, quantity));
        var item = order.OrderItems.Single();
        var handler = new UpdateOrderHandler(db.Orders, db.UnitOfWork, db.Inventory, db.Warehouses,
            db.IssueOrders, db.Approvals, new TestCurrentUser(db.Sales.Id), db.Clock);
        var command = new UpdateOrderCommand(order.Id,
            new OrderInput.Order(db.Client.Id, OrderSource.Online, DeliveryMethod.Pickup, null, null,
                [
                    new OrderInput.Item(db.Product.Id, db.Product.Name.Value, quantity, 10m, 0m,
                        null, null, [], item.Id,
                        [new OrderInput.Allocation(AllocationSourceKind.Warehouse, quantity, db.MainWarehouse.Id)])
                ],
                [], null),
            1);

        var result = await handler.Handle(command, default);
        Assert.True(result.IsSuccess);

        db.Context.ChangeTracker.Clear();
        int allocationId = db.Context.OrderInventoryAllocations.AsNoTracking()
            .Single(allocation => allocation.OrderId == order.Id)
            .Id;
        return (order.Id, result.Value.Version, allocationId);
    }
}
