using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Approvals.DecideApproval;
using OrderHub.Application.Features.Inventory.Approvals.GetApprovals;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Features.Inventory.IssueOrders.CreateStockIssueOrder;
using OrderHub.Application.Features.Inventory.IssueOrders.RecordIssueBatch;
using OrderHub.Application.Features.Inventory.IssueOrders.RequestIssueOrderCancel;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Update;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class ApprovalFlowTests
{
    [Fact]
    public async Task Edit_with_an_open_issue_order_requires_a_reason_and_queues_nothing()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);

        var result = await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 1, reason: null),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("ApprovalRequired", result.ErrorCode);
        Assert.Empty(db.Context.InventoryApprovalRequests);
        Assert.Equal(5, ReloadItemQuantity(db, issued.OrderId));
    }

    [Fact]
    public async Task Edit_with_a_reason_stores_a_pending_payload_and_leaves_the_order_untouched()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);

        var result = await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 1, reason: "تخفيض كمية العميل"),
            default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.PendingApproval);
        Assert.Equal(issued.Version, result.Value.Version);
        Assert.Equal(5, ReloadItemQuantity(db, issued.OrderId));
        Assert.Equal(3, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);

        var approval = Approval(db);
        Assert.Equal(InventoryApprovalRequestType.AllocationEdit, approval.RequestType);
        Assert.Equal(InventoryApprovalStatus.Pending, approval.Status);
        Assert.Equal(issued.Version, approval.TargetVersion);
        Assert.Equal(db.Sales.Id, approval.RequestedByUserId);
        Assert.Equal("تخفيض كمية العميل", approval.Reason);
        Assert.False(string.IsNullOrWhiteSpace(approval.Payload));
    }

    [Fact]
    public async Task A_second_edit_while_the_first_is_pending_is_rejected()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);
        Assert.True((await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 1, reason: "تخفيض"), default)).IsSuccess);

        db.Context.ChangeTracker.Clear();
        var second = await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 2, reason: "تخفيض آخر"),
            default);

        Assert.False(second.IsSuccess);
        Assert.Equal("Conflict", second.ErrorCode);
        Assert.Contains("قيد الاعتماد", second.ErrorMessage);
        Assert.Equal(1, db.Context.InventoryApprovalRequests.Count());
    }

    [Fact]
    public async Task Approved_edit_preserves_issued_quantities_and_records_a_pending_return()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);
        Assert.True((await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 1, reason: "تخفيض"), default)).IsSuccess);

        var decision = await DecideHandler(db, db.Keeper.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, "معتمد", 0),
            default);

        Assert.True(decision.IsSuccess);
        db.Context.ChangeTracker.Clear();

        Assert.Equal(1, ReloadItemQuantity(db, issued.OrderId));
        var allocation = db.Context.OrderInventoryAllocations.AsNoTracking()
            .Single(item => item.Id == issued.AllocationId);
        Assert.Equal(2, allocation.Qty);
        Assert.Equal(2, allocation.IssuedQty);
        Assert.Equal(1, allocation.PendingReturnQty);

        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(8, balance.StockOnHand);
        Assert.Equal(0, balance.ReservedOnHand);

        Assert.Equal(WorkRecordStatus.Cancelled, IssueOrderStatus(db, issued.IssueOrderId));
        var approval = Approval(db);
        Assert.Equal(InventoryApprovalStatus.Approved, approval.Status);
        Assert.Equal("معتمد", approval.DecisionNote);
        Assert.Equal(db.Keeper.Id, approval.DecidedByUserId);
        Assert.Equal(1, approval.Version);
    }

    [Fact]
    public async Task Failed_new_reservation_keeps_the_order_and_reservation_untouched()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);

        var overReserve = new UpdateOrderCommand(
            issued.OrderId,
            new OrderInput.Order(db.Client.Id, OrderSource.Online, DeliveryMethod.Pickup, null, null,
                [
                    new OrderInput.Item(db.Product.Id, db.Product.Name.Value, 2, 10m, 0m, null, null, [],
                        issued.OrderItemId,
                        [
                            new OrderInput.Allocation(AllocationSourceKind.Warehouse, 1, db.MainWarehouse.Id),
                            new OrderInput.Allocation(AllocationSourceKind.Warehouse, 1, db.SecondWarehouse.Id)
                        ])
                ],
                [], null),
            issued.Version,
            "نقل جزئي للمخزن الثاني");

        Assert.True((await UpdateHandler(db).Handle(overReserve, default)).IsSuccess);

        var decision = await DecideHandler(db, db.Keeper.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 0),
            default);

        Assert.False(decision.IsSuccess);
        Assert.Equal("Conflict", decision.ErrorCode);
        db.Context.ChangeTracker.Clear();

        Assert.Equal(InventoryApprovalStatus.Pending, Approval(db).Status);
        Assert.Equal(5, ReloadItemQuantity(db, issued.OrderId));
        var allocation = db.Context.OrderInventoryAllocations.AsNoTracking()
            .Single(item => item.Id == issued.AllocationId);
        Assert.Equal(5, allocation.Qty);
        Assert.Equal(2, allocation.IssuedQty);
        Assert.Equal(3, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
    }

    [Fact]
    public async Task Self_approval_of_an_edit_is_forbidden()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);
        Assert.True((await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 1, reason: "تخفيض"), default)).IsSuccess);

        var decision = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 0),
            default);

        Assert.False(decision.IsSuccess);
        Assert.Equal("Forbidden", decision.ErrorCode);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryApprovalStatus.Pending, Approval(db).Status);
        Assert.Equal(5, ReloadItemQuantity(db, issued.OrderId));
    }

    [Fact]
    public async Task Second_decision_conflicts_without_side_effects()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);
        Assert.True((await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 1, reason: "تخفيض"), default)).IsSuccess);
        int approvalId = Approval(db).Id;

        Assert.True((await DecideHandler(db, db.Keeper.Id).Handle(
            new DecideApprovalCommand(approvalId, true, null, 0), default)).IsSuccess);

        db.Context.ChangeTracker.Clear();
        var again = await DecideHandler(db, db.Keeper.Id).Handle(
            new DecideApprovalCommand(approvalId, false, "تراجع", 1),
            default);

        Assert.False(again.IsSuccess);
        Assert.Equal("Conflict", again.ErrorCode);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(InventoryApprovalStatus.Approved, Approval(db).Status);
        Assert.Equal(1, ReloadItemQuantity(db, issued.OrderId));
    }

    [Fact]
    public async Task Approved_cancel_releases_only_unissued_quantities_and_closes_the_issue_order()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);

        var requested = await CancelHandler(db).Handle(
            new RequestIssueOrderCancelCommand(issued.IssueOrderId, "الكمية غير متوفرة"),
            default);
        Assert.True(requested.IsSuccess);

        var approval = Approval(db);
        Assert.Equal(InventoryApprovalRequestType.IssueOrderCancel, approval.RequestType);
        Assert.Equal(issued.IssueOrderId, approval.StockIssueOrderId);

        var decision = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(approval.Id, true, "تم الإلغاء", 0),
            default);

        Assert.True(decision.IsSuccess);
        db.Context.ChangeTracker.Clear();

        Assert.Equal(WorkRecordStatus.Cancelled, IssueOrderStatus(db, issued.IssueOrderId));
        var allocation = db.Context.OrderInventoryAllocations.AsNoTracking()
            .Single(item => item.Id == issued.AllocationId);
        Assert.Equal(2, allocation.Qty);
        Assert.Equal(2, allocation.IssuedQty);
        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Equal(8, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
    }

    [Fact]
    public async Task Rejected_cancel_restores_the_issue_order()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);
        Assert.True((await CancelHandler(db).Handle(
            new RequestIssueOrderCancelCommand(issued.IssueOrderId, "الكمية غير متوفرة"), default)).IsSuccess);

        var decision = await DecideHandler(db, db.Sales.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, false, "الكمية صرفت بالفعل", 0),
            default);

        Assert.True(decision.IsSuccess);
        db.Context.ChangeTracker.Clear();

        Assert.Equal(WorkRecordStatus.PartiallyExecuted, IssueOrderStatus(db, issued.IssueOrderId));
        Assert.Equal(3, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Equal(InventoryApprovalStatus.Rejected, Approval(db).Status);
        Assert.Equal("الكمية صرفت بالفعل", Approval(db).DecisionNote);
    }

    [Fact]
    public async Task Self_approval_of_a_cancel_request_is_forbidden()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);
        Assert.True((await CancelHandler(db).Handle(
            new RequestIssueOrderCancelCommand(issued.IssueOrderId, "الكمية غير متوفرة"), default)).IsSuccess);

        var decision = await DecideHandler(db, db.Keeper.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 0),
            default);

        Assert.False(decision.IsSuccess);
        Assert.Equal("Forbidden", decision.ErrorCode);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(WorkRecordStatus.CancelRequested, IssueOrderStatus(db, issued.IssueOrderId));
        Assert.Equal(InventoryApprovalStatus.Pending, Approval(db).Status);
    }

    [Fact]
    public async Task Drifted_order_marks_the_edit_request_stale_without_touching_the_order()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);
        Assert.True((await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 1, reason: "تخفيض"), default)).IsSuccess);

        var order = await db.Orders.GetForUpdateAsync(issued.OrderId, default);
        order!.TouchForEdit();
        await db.UnitOfWork.SaveChangesAsync(default);
        db.Context.ChangeTracker.Clear();

        var decision = await DecideHandler(db, db.Keeper.Id).Handle(
            new DecideApprovalCommand(Approval(db).Id, true, null, 0),
            default);

        Assert.False(decision.IsSuccess);
        Assert.Equal("Stale", decision.ErrorCode);
        db.Context.ChangeTracker.Clear();

        var approval = Approval(db);
        Assert.Equal(InventoryApprovalStatus.Stale, approval.Status);
        Assert.False(string.IsNullOrWhiteSpace(approval.DecisionNote));
        Assert.Equal(5, ReloadItemQuantity(db, issued.OrderId));
        Assert.Equal(3, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Equal(WorkRecordStatus.PartiallyExecuted, IssueOrderStatus(db, issued.IssueOrderId));
    }

    [Fact]
    public async Task Queue_lists_pending_requests_with_their_target_labels()
    {
        using var db = new OrderAllocationTestDb();
        var issued = await IssuedAsync(db);
        Assert.True((await UpdateHandler(db).Handle(
            EditCommand(db, issued, quantity: 1, reason: "تخفيض"), default)).IsSuccess);
        Assert.True((await CancelHandler(db).Handle(
            new RequestIssueOrderCancelCommand(issued.IssueOrderId, "إلغاء"), default)).IsSuccess);

        var result = await new GetApprovalsQueryHandler(
            db.Approvals,
            new StubPermissions((_, level) => level == PermissionLevel.Approve),
            new TestCurrentUser(db.Keeper.Id)).Handle(
            new GetApprovalsQuery(InventoryApprovalStatus.Pending),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        var edit = result.Value.Single(row => row.RequestType == InventoryApprovalRequestType.AllocationEdit);
        Assert.False(string.IsNullOrWhiteSpace(edit.OrderNumber));
        Assert.Equal("مبيعات", edit.RequestedByName);
        var cancel = result.Value.Single(row => row.RequestType == InventoryApprovalRequestType.IssueOrderCancel);
        Assert.True(cancel.IssueOrderNumber > 0);
        Assert.Equal(db.MainWarehouse.Name.Value, cancel.IssueOrderWarehouseName);
        Assert.False(string.IsNullOrWhiteSpace(cancel.IssueOrderOrderNumber));
    }

    private sealed record IssuedOrder(int OrderId, int Version, int AllocationId, int IssueOrderId, int OrderItemId);

    private static UpdateOrderHandler UpdateHandler(OrderAllocationTestDb db) =>
        new(db.Orders, db.UnitOfWork, db.Inventory, db.Warehouses, db.IssueOrders, db.Approvals,
            new TestCurrentUser(db.Sales.Id), db.Clock);

    private static DecideApprovalCommandHandler DecideHandler(OrderAllocationTestDb db, int userId) =>
        new(db.Approvals, db.Orders, db.IssueOrders, db.Inventory, db.Warehouses, db.UnitOfWork,
            new TestCurrentUser(userId), db.Clock);

    private static RequestIssueOrderCancelCommandHandler CancelHandler(OrderAllocationTestDb db) =>
        new(db.IssueOrders, db.Approvals, db.UnitOfWork, new TestCurrentUser(db.Keeper.Id), db.Clock);

    private static CreateStockIssueOrderCommandHandler CreateIssueOrderHandler(OrderAllocationTestDb db) =>
        new(db.Orders, db.Warehouses, db.IssueOrders, db.Inventory, db.UnitOfWork,
            new TestCurrentUser(db.Sales.Id), db.Clock);

    private static RecordIssueBatchCommandHandler BatchHandler(OrderAllocationTestDb db) =>
        new(db.IssueOrders, db.Inventory, db.UnitOfWork, new TestCurrentUser(db.Keeper.Id), db.Clock);

    private static async Task<IssuedOrder> IssuedAsync(OrderAllocationTestDb db, int quantity = 5, int issued = 2)
    {
        var (orderId, version, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity);

        var created = await CreateIssueOrderHandler(db).Handle(
            new CreateStockIssueOrderCommand(new StockIssueOrderInput(
                orderId,
                db.MainWarehouse.Id,
                [new StockIssueOrderLineInput(allocationId, quantity)])),
            default);
        Assert.True(created.IsSuccess);

        db.Context.ChangeTracker.Clear();
        int lineId = db.Context.StockIssueLines.AsNoTracking()
            .Single(line => line.IssueOrderId == created.Value)
            .Id;
        var batch = await BatchHandler(db).Handle(
            new RecordIssueBatchCommand(created.Value, false, default, [new IssueBatchLineInput(lineId, issued, null)]),
            default);
        Assert.True(batch.IsSuccess);

        db.Context.ChangeTracker.Clear();
        int orderItemId = db.Context.OrderItems.AsNoTracking()
            .Single(item => item.OrderId == orderId)
            .Id;
        return new IssuedOrder(orderId, version, allocationId, created.Value, orderItemId);
    }

    private static UpdateOrderCommand EditCommand(
        OrderAllocationTestDb db,
        IssuedOrder issued,
        int quantity,
        string? reason) =>
        new(
            issued.OrderId,
            new OrderInput.Order(
                db.Client.Id,
                OrderSource.Online,
                DeliveryMethod.Pickup,
                null,
                null,
                [
                    new OrderInput.Item(db.Product.Id, db.Product.Name.Value, quantity, 10m, 0m, null, null, [],
                        issued.OrderItemId,
                        [new OrderInput.Allocation(AllocationSourceKind.Warehouse, quantity, db.MainWarehouse.Id)])
                ],
                [],
                null),
            issued.Version,
            reason);

    private static InventoryApprovalRequest Approval(OrderAllocationTestDb db) =>
        db.Context.InventoryApprovalRequests.AsNoTracking().Single();

    private static int ReloadItemQuantity(OrderAllocationTestDb db, int orderId) =>
        db.Context.OrderItems.AsNoTracking().Single(item => item.OrderId == orderId).Quantity;

    private static WorkRecordStatus IssueOrderStatus(OrderAllocationTestDb db, int issueOrderId) =>
        db.Context.StockIssueOrders.AsNoTracking().Single(order => order.Id == issueOrderId).Status;
}
