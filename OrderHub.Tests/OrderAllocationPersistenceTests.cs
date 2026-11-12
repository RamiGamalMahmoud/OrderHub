using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class OrderAllocationPersistenceTests
{
    [Fact]
    public async Task Allocations_round_trip_with_warehouse_and_supplier_sources()
    {
        using var db = new OrderAllocationTestDb();
        var order = db.NewOrder("ORD-ALLOC-1", (db.Product, 4));

        var loaded = await db.Orders.GetForUpdateAsync(order.Id);
        var item = loaded.OrderItems.Single();
        var warehouse = OrderInventoryAllocation.Create(
            item.ProductId, AllocationSourceKind.Warehouse, 3, warehouseId: db.MainWarehouse.Id).Value;
        var supplier = OrderInventoryAllocation.Create(
            item.ProductId, AllocationSourceKind.Supplier, 1, supplierId: db.Supplier.Id).Value;
        loaded.AddAllocation(warehouse, item);
        loaded.AddAllocation(supplier, item);
        await db.UnitOfWork.SaveChangesAsync(CancellationToken.None);
        db.Context.ChangeTracker.Clear();

        var reloaded = await db.Orders.GetForUpdateAsync(order.Id);
        var saved = reloaded.Allocations.OrderBy(allocation => allocation.Id).ToList();
        Assert.Equal(2, saved.Count);
        Assert.All(saved, allocation => Assert.Equal(reloaded.OrderItems.Single().Id, allocation.OrderItemId));
        Assert.Equal(AllocationSourceKind.Warehouse, saved[0].SourceKind);
        Assert.Equal(db.MainWarehouse.Id, saved[0].WarehouseId);
        Assert.Null(saved[0].SupplierId);
        Assert.Equal(3, saved[0].Qty);
        Assert.Equal(OrderAllocationStatus.Active, saved[0].Status);
        Assert.Equal(AllocationSourceKind.Supplier, saved[1].SourceKind);
        Assert.Equal(db.Supplier.Id, saved[1].SupplierId);
        Assert.Null(saved[1].WarehouseId);
        Assert.Equal(1, saved[1].Qty);
    }

    [Fact]
    public async Task Foreign_key_restrict_blocks_deleting_an_allocated_order_item()
    {
        using var db = new OrderAllocationTestDb();
        var allocation = await SeedAllocatedOrderAsync(db);
        var itemId = allocation.OrderItemId;

        var error = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
            db.Context.Database.ExecuteSqlRawAsync($"DELETE FROM order_items WHERE id = {itemId}"));

        Assert.Contains("FOREIGN KEY", error.Message, StringComparison.OrdinalIgnoreCase);

        await db.Context.Database.ExecuteSqlRawAsync(
            $"DELETE FROM order_inventory_allocations WHERE id = {allocation.Id}");
        var affected = await db.Context.Database.ExecuteSqlRawAsync(
            $"DELETE FROM order_items WHERE id = {itemId}");
        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task Unique_index_blocks_a_duplicate_warehouse_allocation_per_item()
    {
        using var db = new OrderAllocationTestDb();
        var allocation = await SeedAllocatedOrderAsync(db);

        var error = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
            db.Context.Database.ExecuteSqlRawAsync($"""
                INSERT INTO order_inventory_allocations
                    (order_id, order_item_id, product_id, source_kind, warehouse_id, supplier_id,
                     qty, issued_qty, status, created_at)
                VALUES
                    ({allocation.OrderId}, {allocation.OrderItemId}, {allocation.ProductId}, 'Warehouse',
                     {allocation.WarehouseId}, NULL, 1, 0, 'Active', CURRENT_TIMESTAMP)
                """));

        Assert.Contains("UNIQUE", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Check_constraint_blocks_a_non_positive_allocation_quantity()
    {
        using var db = new OrderAllocationTestDb();
        var allocation = await SeedAllocatedOrderAsync(db);

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
            db.Context.Database.ExecuteSqlRawAsync($"""
                INSERT INTO order_inventory_allocations
                    (order_id, order_item_id, product_id, source_kind, warehouse_id, supplier_id,
                     qty, issued_qty, status, created_at)
                VALUES
                    ({allocation.OrderId}, {allocation.OrderItemId}, {allocation.ProductId}, 'Warehouse',
                     {allocation.WarehouseId}, NULL, 0, 0, 'Active', CURRENT_TIMESTAMP)
                """));
    }

    [Fact]
    public async Task Availability_reports_active_warehouses_descending_and_excludes_inactive_ones()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 10);
        db.StockIn(db.SecondWarehouse, db.Product, 5);
        db.StockIn(db.InactiveWarehouse, db.Product, 4);
        db.SeedReservation(db.MainWarehouse, db.Product, 3);

        var rows = await db.Inventory.GetAvailabilityAsync(db.Product.Id);

        Assert.Equal(2, rows.Count);
        Assert.Equal(db.MainWarehouse.Id, rows[0].WarehouseId);
        Assert.Equal(10, rows[0].StockOnHand);
        Assert.Equal(3, rows[0].ReservedOnHand);
        Assert.Equal(7, rows[0].Available);
        Assert.Equal(db.SecondWarehouse.Id, rows[1].WarehouseId);
        Assert.Equal(5, rows[1].Available);
    }

    [Fact]
    public async Task Availability_includes_an_active_warehouse_without_a_balance_row()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 2);

        var rows = await db.Inventory.GetAvailabilityAsync(db.Product.Id);

        var second = rows.Single(row => row.WarehouseId == db.SecondWarehouse.Id);
        Assert.Equal(0, second.StockOnHand);
        Assert.Equal(0, second.Available);
    }

    private static async Task<OrderInventoryAllocation> SeedAllocatedOrderAsync(OrderAllocationTestDb db)
    {
        db.StockIn(db.MainWarehouse, db.Product, 5);
        var order = db.NewOrder("ORD-ALLOC-P", (db.Product, 2));
        var loaded = await db.Orders.GetForUpdateAsync(order.Id);
        var item = loaded.OrderItems.Single();
        var allocation = OrderInventoryAllocation.Create(
            item.ProductId, AllocationSourceKind.Warehouse, 2, warehouseId: db.MainWarehouse.Id).Value;
        loaded.AddAllocation(allocation, item);
        await db.UnitOfWork.SaveChangesAsync(CancellationToken.None);
        db.Context.ChangeTracker.Clear();
        return allocation;
    }
}
