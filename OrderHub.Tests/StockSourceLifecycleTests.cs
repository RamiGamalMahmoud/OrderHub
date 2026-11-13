using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Orders.Allocations;
using OrderHub.Application.Features.Orders.Archive;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Application.Features.Orders.Update;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class StockSourceLifecycleTests
{
    [Fact]
    public async Task The_second_saver_of_the_last_units_gets_conflict_without_a_partial_save()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 3);
        var first = db.NewOrder("ORD-RACE-A", (db.Product, 1));
        var second = db.NewOrder("ORD-RACE-B", (db.Product, 1));
        var firstItem = first.OrderItems.Single();
        var secondItem = second.OrderItems.Single();

        var winner = await UpdateHandler(db).Handle(
            Command(db, first.Id, 1, Item(db, db.Product, 3, firstItem.Id,
                [Warehouse(db.MainWarehouse, 3)])), default);
        Assert.True(winner.IsSuccess);

        db.Context.ChangeTracker.Clear();
        var loser = await UpdateHandler(db).Handle(
            Command(db, second.Id, 1, Item(db, db.Product, 3, secondItem.Id,
                [Warehouse(db.MainWarehouse, 3)])), default);

        Assert.False(loser.IsSuccess);
        Assert.Equal("Conflict", loser.ErrorCode);
        Assert.Contains("المخزن الرئيسي", loser.ErrorMessage);
        Assert.Contains("أعد التوزيع", loser.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(3, balance.StockOnHand);
        Assert.Equal(3, balance.ReservedOnHand);

        var untouched = await db.Orders.GetForUpdateAsync(second.Id);
        Assert.Equal(1, untouched.OrderItems.Single().Quantity);
        Assert.Empty(untouched.Allocations);
    }

    [Fact]
    public async Task Update_keeps_item_ids_and_resizes_the_reservation()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 10);
        var order = db.NewOrder("ORD-KEEP", (db.Product, 2), (db.SecondProduct, 3));
        var gloveId = order.OrderItems.Single(item => item.ProductId == db.Product.Id).Id;
        var maskId = order.OrderItems.Single(item => item.ProductId == db.SecondProduct.Id).Id;

        var result = await UpdateHandler(db).Handle(Command(db, order.Id, 1,
            Item(db, db.Product, 5, gloveId, [Warehouse(db.MainWarehouse, 5)]),
            Item(db, db.SecondProduct, 3, maskId, [SupplierAllocation(db, 3)])), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.OrderItems.Count);
        Assert.Equal(gloveId, result.Value.OrderItems.Single(item => item.ProductId == db.Product.Id).Id);
        Assert.Equal(maskId, result.Value.OrderItems.Single(item => item.ProductId == db.SecondProduct.Id).Id);

        db.Context.ChangeTracker.Clear();
        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(5, balance.ReservedOnHand);
        Assert.Equal(10, balance.StockOnHand);

        var reloaded = await db.Orders.GetForUpdateAsync(order.Id);
        Assert.Equal(new[] { gloveId, maskId }.OrderBy(id => id),
            reloaded.OrderItems.Select(item => item.Id).OrderBy(id => id));
        var gloveAllocation = reloaded.Allocations.Single(allocation => allocation.OrderItemId == gloveId);
        Assert.Equal(5, gloveAllocation.Qty);
        Assert.Equal(db.MainWarehouse.Id, gloveAllocation.WarehouseId);
        var maskAllocation = reloaded.Allocations.Single(allocation => allocation.OrderItemId == maskId);
        Assert.Equal(AllocationSourceKind.Supplier, maskAllocation.SourceKind);
        Assert.Equal(db.Supplier.Id, maskAllocation.SupplierId);
    }

    [Fact]
    public async Task Removing_an_allocated_item_releases_its_reservation()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 10);
        db.StockIn(db.MainWarehouse, db.SecondProduct, 10);
        var order = db.NewOrder("ORD-REMOVE", (db.Product, 2), (db.SecondProduct, 3));
        var gloveId = order.OrderItems.Single(item => item.ProductId == db.Product.Id).Id;
        var maskId = order.OrderItems.Single(item => item.ProductId == db.SecondProduct.Id).Id;

        var seed = await UpdateHandler(db).Handle(Command(db, order.Id, 1,
            Item(db, db.Product, 2, gloveId, [Warehouse(db.MainWarehouse, 2)]),
            Item(db, db.SecondProduct, 3, maskId, [Warehouse(db.MainWarehouse, 3)])), default);
        Assert.True(seed.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(5, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand +
            db.ReloadBalance(db.MainWarehouse, db.SecondProduct).ReservedOnHand);

        var removed = await UpdateHandler(db).Handle(Command(db, order.Id, seed.Value.Version,
            Item(db, db.Product, 2, gloveId, [Warehouse(db.MainWarehouse, 2)])), default);

        Assert.True(removed.IsSuccess, removed.ErrorMessage);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(2, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.SecondProduct).ReservedOnHand);
        var reloaded = await db.Orders.GetForUpdateAsync(order.Id);
        Assert.Single(reloaded.OrderItems);
        Assert.Single(reloaded.Allocations);
    }

    [Fact]
    public async Task Removing_an_item_with_issued_quantity_is_blocked()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 10);
        db.StockIn(db.MainWarehouse, db.SecondProduct, 10);
        var order = db.NewOrder("ORD-ISSUED", (db.Product, 2), (db.SecondProduct, 3));
        var gloveId = order.OrderItems.Single(item => item.ProductId == db.Product.Id).Id;
        var maskId = order.OrderItems.Single(item => item.ProductId == db.SecondProduct.Id).Id;
        var seed = await UpdateHandler(db).Handle(Command(db, order.Id, 1,
            Item(db, db.Product, 2, gloveId, [Warehouse(db.MainWarehouse, 2)]),
            Item(db, db.SecondProduct, 3, maskId, [Warehouse(db.MainWarehouse, 3)])), default);
        Assert.True(seed.IsSuccess);

        await db.Context.Database.ExecuteSqlRawAsync(
            $"UPDATE order_inventory_allocations SET issued_qty = 1 WHERE order_item_id = {maskId}");
        db.Context.ChangeTracker.Clear();

        var removed = await UpdateHandler(db).Handle(Command(db, order.Id, seed.Value.Version,
            Item(db, db.Product, 2, gloveId, [Warehouse(db.MainWarehouse, 2)])), default);

        Assert.False(removed.IsSuccess);
        Assert.Contains("صُرفت منه كميات", removed.ErrorMessage);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(2, (await db.Orders.GetForUpdateAsync(order.Id)).OrderItems.Count);
    }

    [Fact]
    public async Task Legacy_order_resave_requires_a_source_for_every_item()
    {
        using var db = new OrderAllocationTestDb();
        var order = db.NewOrder("ORD-LEGACY", (db.Product, 2));
        var item = order.OrderItems.Single();

        var result = await UpdateHandler(db).Handle(Command(db, order.Id, 1,
            new OrderInput.Item(db.Product.Id, db.Product.Name.Value, 2, 10m, 0m, null, null, [],
                item.Id, null)), default);

        Assert.False(result.IsSuccess);
        Assert.Contains(OrderAllocationRules.MissingSourceMessage, result.ErrorMessage);
        db.Context.ChangeTracker.Clear();
        Assert.Empty((await db.Orders.GetForUpdateAsync(order.Id)).Allocations);
    }

    [Fact]
    public async Task Supplier_only_sources_create_no_reservation_or_balance_row()
    {
        using var db = new OrderAllocationTestDb();
        var order = db.NewOrder("ORD-SUPPLIER", (db.Product, 4));
        var item = order.OrderItems.Single();

        var result = await UpdateHandler(db).Handle(Command(db, order.Id, 1,
            Item(db, db.Product, 4, item.Id, [SupplierAllocation(db, 4)])), default);

        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Null(db.ReloadBalance(db.MainWarehouse, db.Product));
        var reloaded = await db.Orders.GetForUpdateAsync(order.Id);
        Assert.Equal(db.Supplier.Id, reloaded.OrderItems.Single().SupplierId);
        Assert.Equal(AllocationSourceKind.Supplier, reloaded.Allocations.Single().SourceKind);
        Assert.Equal(0, reloaded.Allocations.Single().IssuedQty);
    }

    [Fact]
    public async Task Mixed_sources_reserve_only_the_warehouse_share()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 10);
        var order = db.NewOrder("ORD-MIXED", (db.Product, 2));
        var item = order.OrderItems.Single();

        var result = await UpdateHandler(db).Handle(Command(db, order.Id, 1,
            Item(db, db.Product, 6, item.Id,
                [Warehouse(db.MainWarehouse, 4), SupplierAllocation(db, 2)])), default);

        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(4, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Equal(2, (await db.Orders.GetForUpdateAsync(order.Id)).Allocations.Count);
    }

    [Fact]
    public async Task Delivered_order_releases_the_un_issued_remainder()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version) = await ReserveOrderAsync(db, quantity: 3);
        var lifecycle = Lifecycle(db);

        var ready = await lifecycle.Handle(new ChangeOrderStatusCommand(orderId, version, OrderStatus.ReadyForPickup), default);
        Assert.True(ready.IsSuccess);
        var delivered = await lifecycle.Handle(
            new ChangeOrderStatusCommand(orderId, ready.Value.Version, OrderStatus.Delivered), default);
        Assert.True(delivered.IsSuccess);

        db.Context.ChangeTracker.Clear();
        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(0, balance.ReservedOnHand);
        Assert.Equal(10, balance.StockOnHand);
        Assert.Equal(OrderAllocationStatus.Released,
            db.Context.OrderInventoryAllocations.AsNoTracking().Single().Status);
        var history = db.Context.OrderStatusHistories.AsNoTracking()
            .Single(row => row.Source == OrderHistorySource.StockRelease);
        Assert.Null(history.PreviousStatus);
        Assert.Equal("إتمام التسليم", history.Reason);
        Assert.Equal(db.Sales.Id, history.ChangedByUserId);
    }

    [Fact]
    public async Task Cancelled_order_releases_the_un_issued_remainder()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version) = await ReserveOrderAsync(db, quantity: 3);

        var cancelled = await Lifecycle(db).Handle(
            new ChangeOrderStatusCommand(orderId, version, OrderStatus.Cancelled, "طلب العميل الإلغاء"), default);

        Assert.True(cancelled.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Equal(OrderAllocationStatus.Released,
            db.Context.OrderInventoryAllocations.AsNoTracking().Single().Status);
        Assert.Equal("إلغاء الطلب", db.Context.OrderStatusHistories.AsNoTracking()
            .Single(row => row.Source == OrderHistorySource.StockRelease).Reason);
    }

    [Fact]
    public async Task Archived_order_releases_the_un_issued_remainder()
    {
        using var db = new OrderAllocationTestDb();
        var (orderId, version) = await ReserveOrderAsync(db, quantity: 3);

        var archived = await Lifecycle(db).Handle(new ArchiveOrderCommand(orderId, version, "تنظيف الطلبات"), default);

        Assert.True(archived.IsSuccess);
        db.Context.ChangeTracker.Clear();
        Assert.Equal(0, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        Assert.Equal(10, db.ReloadBalance(db.MainWarehouse, db.Product).StockOnHand);
        Assert.Equal("أرشفة الطلب", db.Context.OrderStatusHistories.AsNoTracking()
            .Single(row => row.Source == OrderHistorySource.StockRelease).Reason);
    }

    [Fact]
    public async Task Legacy_orders_close_without_touching_stock()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 10);
        var order = db.NewOrder("ORD-LEGACY-CLOSE", (db.Product, 2));

        var cancelled = await Lifecycle(db).Handle(
            new ChangeOrderStatusCommand(order.Id, 1, OrderStatus.Cancelled, "إلغاء"), default);

        Assert.True(cancelled.IsSuccess);
        db.Context.ChangeTracker.Clear();
        var balance = db.ReloadBalance(db.MainWarehouse, db.Product);
        Assert.Equal(0, balance.ReservedOnHand);
        Assert.Equal(10, balance.StockOnHand);
        Assert.DoesNotContain(db.Context.OrderStatusHistories.AsNoTracking(),
            row => row.Source == OrderHistorySource.StockRelease);
    }

    private static async Task<(int OrderId, int Version)> ReserveOrderAsync(OrderAllocationTestDb db, int quantity)
    {
        db.StockIn(db.MainWarehouse, db.Product, 10);
        var order = db.NewOrder("ORD-LIFE-" + Guid.NewGuid().ToString("N"), (db.Product, quantity));
        var item = order.OrderItems.Single();
        var result = await UpdateHandler(db).Handle(Command(db, order.Id, 1,
            Item(db, db.Product, quantity, item.Id, [Warehouse(db.MainWarehouse, quantity)])), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(quantity, db.ReloadBalance(db.MainWarehouse, db.Product).ReservedOnHand);
        db.Context.ChangeTracker.Clear();
        return (order.Id, result.Value.Version);
    }

    private static OrderLifecycleHandlers Lifecycle(OrderAllocationTestDb db) =>
        new(db.Orders, db.UnitOfWork, db.Inventory, db.IssueOrders, new TestCurrentUser(db.Sales.Id), db.Clock);

    private static UpdateOrderHandler UpdateHandler(OrderAllocationTestDb db) =>
        new(db.Orders, db.UnitOfWork, db.Inventory, db.Warehouses, db.IssueOrders, db.Approvals,
            new TestCurrentUser(db.Sales.Id), db.Clock);

    private static UpdateOrderCommand Command(
        OrderAllocationTestDb db, int orderId, int version, params OrderInput.Item[] items) =>
        new(orderId, new OrderInput.Order(db.Client.Id, OrderSource.Online, DeliveryMethod.Pickup,
            null, null, items, [], null), version);

    private static OrderInput.Item Item(
        OrderAllocationTestDb db,
        Product product,
        int quantity,
        int? itemId,
        OrderInput.Allocation[] allocations) =>
        new(product.Id, product.Name.Value, quantity, 10m, 0m,
            allocations.Any(allocation => allocation.SourceKind == AllocationSourceKind.Supplier)
                ? db.Supplier.Name.Value
                : null,
            allocations.Any(allocation => allocation.SourceKind == AllocationSourceKind.Supplier)
                ? db.Supplier.Id
                : null,
            [], itemId, allocations);

    private static OrderInput.Allocation Warehouse(Warehouse warehouse, int quantity) =>
        new(AllocationSourceKind.Warehouse, quantity, warehouse.Id);

    private static OrderInput.Allocation SupplierAllocation(OrderAllocationTestDb db, int quantity) =>
        new(AllocationSourceKind.Supplier, quantity, null, db.Supplier.Id);
}
