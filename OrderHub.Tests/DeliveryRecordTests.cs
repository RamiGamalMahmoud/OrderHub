using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Deliveries.GetOrderDeliveries;
using OrderHub.Application.Features.Inventory.Deliveries.RecordStockDelivery;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Reopsitories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class DeliveryRecordTests
{
    [Fact]
    public async Task Record_caps_deliveries_at_the_issued_quantity_and_aggregates_them()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);

        var first = await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 2, note: "  بضاعة سليمة  "), default);
        Assert.True(first.IsSuccess);

        var second = await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 1), default);
        Assert.True(second.IsSuccess);

        var over = await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 1), default);
        Assert.False(over.IsSuccess);
        Assert.Equal("Conflict", over.ErrorCode);
        Assert.Contains("سُلمت كل الكمية", over.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        var records = db.Context.StockDeliveryRecords.AsNoTracking()
            .OrderBy(record => record.Id)
            .ToList();
        Assert.Equal(2, records.Count);
        Assert.All(records, record =>
        {
            Assert.Equal(orderId, record.OrderId);
            Assert.Equal(allocationId, record.AllocationId);
            Assert.Equal(db.Product.Id, record.ProductId);
            Assert.Equal(db.Keeper.Id, record.RecordedByUserId);
            Assert.Null(record.TripId);
            Assert.Null(record.ShipmentId);
        });
        Assert.Equal(new[] { 2, 1 }, records.Select(record => record.Quantity));
        Assert.Equal("بضاعة سليمة", records[0].Note);
        Assert.Null(records[1].Note);

        var overview = await QueryHandler(db).Handle(new GetOrderDeliveriesQuery(orderId), default);
        Assert.True(overview.IsSuccess);
        var row = Assert.Single(overview.Value.Rows);
        Assert.Equal(allocationId, row.AllocationId);
        Assert.Equal(3, row.IssuedQty);
        Assert.Equal(3, row.DeliveredQty);
        Assert.Equal(0, row.RemainingQty);
        Assert.False(row.CanDeliver);
    }

    [Fact]
    public async Task Record_rejects_a_quantity_above_the_remaining_cap()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);
        await IssueAllAsync(db, orderId, allocationId, quantity: 3);

        Assert.True((await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 1), default)).IsSuccess);

        var over = await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 3), default);

        Assert.False(over.IsSuccess);
        Assert.Equal("Conflict", over.ErrorCode);
        Assert.Contains("المتبقي (2)", over.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(1, db.Context.StockDeliveryRecords.Count());
        Assert.Equal(7, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
    }

    [Fact]
    public async Task Record_rejects_delivery_before_the_issue_batches()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 3);

        var result = await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 1), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("لم تُصرف", result.ErrorMessage);
        Assert.Equal(0, db.Context.StockDeliveryRecords.Count());
    }

    [Fact]
    public async Task Record_rejects_an_allocation_from_another_order()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 2);
        await IssueAllAsync(db, orderId, allocationId, quantity: 2);
        var other = db.NewOrder("ORD-DELIVERY-OTHER-" + Guid.NewGuid().ToString("N"), (db.Product, 1));

        var result = await Handler(db, db.Keeper.Id).Handle(
            Delivery(other.Id, allocationId, 1), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
        Assert.Equal(0, db.Context.StockDeliveryRecords.Count());
    }

    [Fact]
    public async Task Record_rejects_a_link_that_does_not_belong_to_the_order()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 2);
        await IssueAllAsync(db, orderId, allocationId, quantity: 2);

        var missingTrip = await Handler(db, db.Keeper.Id).Handle(
            new RecordStockDeliveryCommand(orderId, allocationId, 1, 987654, null), default);

        Assert.False(missingTrip.IsSuccess);
        Assert.Equal("InvalidOperation", missingTrip.ErrorCode);
        Assert.Contains("لا تخص", missingTrip.ErrorMessage);

        var missingShipment = await Handler(db, db.Keeper.Id).Handle(
            new RecordStockDeliveryCommand(orderId, allocationId, 1, null, 987654), default);

        Assert.False(missingShipment.IsSuccess);
        Assert.Equal(0, db.Context.StockDeliveryRecords.Count());
    }

    [Fact]
    public async Task Record_rejects_cancelled_orders()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 2);
        await IssueAllAsync(db, orderId, allocationId, quantity: 2);

        var lifecycle = new OrderLifecycleHandlers(
            db.Orders, db.UnitOfWork, db.Inventory, db.IssueOrders,
            new TestCurrentUser(db.Sales.Id), db.Clock);
        var cancelled = await lifecycle.Handle(
            new ChangeOrderStatusCommand(orderId, version, OrderStatus.Cancelled, "طلب العميل الإلغاء"),
            default);
        Assert.True(cancelled.IsSuccess);

        var result = await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 1), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("ملغى", result.ErrorMessage);
        Assert.Equal(0, db.Context.StockDeliveryRecords.Count());
    }

    [Fact]
    public async Task Record_requires_an_authenticated_user()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 2);
        await IssueAllAsync(db, orderId, allocationId, quantity: 2);

        var result = await new RecordStockDeliveryCommandHandler(
            db.Orders, Deliveries(db), db.Inventory, db.UnitOfWork,
            TestCurrentUser.Anonymous, db.Clock)
            .Handle(Delivery(orderId, allocationId, 1), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.ErrorCode);
        Assert.Equal(0, db.Context.StockDeliveryRecords.Count());
    }

    [Fact]
    public async Task Record_rejects_a_non_positive_quantity()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 2);
        await IssueAllAsync(db, orderId, allocationId, quantity: 2);

        var result = await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 0), default);

        Assert.False(result.IsSuccess);
        Assert.Contains("أكبر من صفر", result.ErrorMessage);
    }

    [Fact]
    public async Task Record_rejects_a_note_longer_than_the_limit()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, _, allocationId) = await StockIssueOrderTests.ReserveAsync(db, quantity: 2);
        await IssueAllAsync(db, orderId, allocationId, quantity: 2);

        var result = await Handler(db, db.Keeper.Id).Handle(
            Delivery(orderId, allocationId, 1, note: new string('م', 201)), default);

        Assert.False(result.IsSuccess);
        Assert.Contains("200", result.ErrorMessage);
        Assert.Equal(0, db.Context.StockDeliveryRecords.Count());
    }

    private static RecordStockDeliveryCommand Delivery(
        int orderId,
        int allocationId,
        int quantity,
        string? note = null) =>
        new(orderId, allocationId, quantity, null, null, EffectiveAtUtc: null, Note: note);

    private static StockDeliveryRepository Deliveries(OrderAllocationTestDb db) =>
        new(db.Context);

    private static RecordStockDeliveryCommandHandler Handler(OrderAllocationTestDb db, int userId) =>
        new(db.Orders, Deliveries(db), db.Inventory, db.UnitOfWork,
            new TestCurrentUser(userId), db.Clock);

    private static GetOrderDeliveriesQueryHandler QueryHandler(OrderAllocationTestDb db) =>
        new(Deliveries(db), db.Orders);

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
}
