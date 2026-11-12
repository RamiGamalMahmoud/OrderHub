using OrderHub.Application.Features.Orders.Allocations;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;

namespace OrderHub.Tests;

public sealed class OrderAllocationTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Plan_rejects_an_item_without_a_source()
    {
        var result = OrderAllocationRules.Plan([Item(quantity: 2)], allowExistingIds: false);

        Assert.False(result.IsSuccess);
        Assert.Contains(OrderAllocationRules.MissingSourceMessage, result.ErrorMessage);
    }

    [Fact]
    public void Plan_accepts_stock_only_supplier_only_and_mixed_sources()
    {
        var stockOnly = OrderAllocationRules.Plan(
            [Item(quantity: 3, Allocations(Warehouse(1, 3)))], allowExistingIds: false);
        Assert.True(stockOnly.IsSuccess);
        Assert.Null(stockOnly.Value[0].SupplierId);
        Assert.Null(stockOnly.Value[0].SupplierName);

        var supplierOnly = OrderAllocationRules.Plan(
            [Item(quantity: 4, Allocations(SupplierAllocation(7, 4)), supplierId: 7, supplierName: "مورد")],
            allowExistingIds: false);
        Assert.True(supplierOnly.IsSuccess);
        Assert.Equal(7, supplierOnly.Value[0].SupplierId);
        Assert.Equal("مورد", supplierOnly.Value[0].SupplierName);

        var mixed = OrderAllocationRules.Plan(
            [Item(quantity: 5, Allocations(Warehouse(1, 3), SupplierAllocation(7, 2)),
                supplierId: 7, supplierName: "مورد")], allowExistingIds: true);
        Assert.True(mixed.IsSuccess);
        Assert.Equal(2, mixed.Value[0].Allocations.Count);
        Assert.Equal(7, mixed.Value[0].SupplierId);
    }

    [Fact]
    public void Plan_requires_allocations_to_sum_to_the_item_quantity()
    {
        var result = OrderAllocationRules.Plan(
            [Item(quantity: 5, Allocations(Warehouse(1, 3)))], allowExistingIds: false);

        Assert.False(result.IsSuccess);
        Assert.Contains("مجموع كميات المصادر", result.ErrorMessage);
    }

    [Fact]
    public void Plan_rejects_a_duplicate_warehouse_on_the_same_item()
    {
        var result = OrderAllocationRules.Plan(
            [Item(quantity: 4, Allocations(Warehouse(1, 2), Warehouse(1, 2)))], allowExistingIds: false);

        Assert.False(result.IsSuccess);
        Assert.Contains("تكرار المخزن", result.ErrorMessage);
    }

    [Fact]
    public void Plan_rejects_more_than_one_supplier_allocation()
    {
        var result = OrderAllocationRules.Plan(
            [Item(quantity: 4, Allocations(SupplierAllocation(7, 2), SupplierAllocation(8, 2)),
                supplierId: 7, supplierName: "مورد")], allowExistingIds: false);

        Assert.False(result.IsSuccess);
        Assert.Contains("أكثر من", result.ErrorMessage);
    }

    [Fact]
    public void Plan_rejects_supplier_allocation_that_disagrees_with_the_item_supplier()
    {
        var result = OrderAllocationRules.Plan(
            [Item(quantity: 2, Allocations(SupplierAllocation(7, 2)), supplierId: 8, supplierName: "آخر")],
            allowExistingIds: false);

        Assert.False(result.IsSuccess);
        Assert.Contains("لا يطابق", result.ErrorMessage);
    }

    [Fact]
    public void Plan_forbids_item_ids_on_creation()
    {
        var result = OrderAllocationRules.Plan(
            [Item(quantity: 1, Allocations(Warehouse(1, 1)), itemId: 5)], allowExistingIds: false);

        Assert.False(result.IsSuccess);
        Assert.Contains("معرف البند غير صالح", result.ErrorMessage);
    }

    [Fact]
    public void Allocation_rejects_invalid_source_and_quantity()
    {
        Assert.False(OrderInventoryAllocation.Create(1, AllocationSourceKind.Warehouse, 1).IsSuccess);
        Assert.False(OrderInventoryAllocation.Create(1, AllocationSourceKind.Supplier, 1).IsSuccess);
        Assert.False(OrderInventoryAllocation.Create(1, AllocationSourceKind.Warehouse, 0, warehouseId: 1).IsSuccess);
        Assert.False(OrderInventoryAllocation.Create(0, AllocationSourceKind.Warehouse, 1, warehouseId: 1).IsSuccess);
        Assert.True(OrderInventoryAllocation.Create(1, AllocationSourceKind.Warehouse, 2, warehouseId: 3).IsSuccess);
    }

    [Fact]
    public void Release_hook_marks_active_allocations_and_writes_one_system_row()
    {
        var order = NewOrderWithItem();
        var item = order.OrderItems.Single();
        var allocation = OrderInventoryAllocation.Create(item.ProductId, AllocationSourceKind.Warehouse, 2, warehouseId: 4).Value;
        order.AddAllocation(allocation, item);

        var result = order.ReleaseOpenAllocations(new OrderOperation(3, "النظام", Now), "إلغاء الطلب");

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderAllocationStatus.Released, allocation.Status);
        var history = order.StatusHistory.Last();
        Assert.Equal(OrderHistorySource.StockRelease, history.Source);
        Assert.Null(history.PreviousStatus);
        Assert.Equal(OrderStatus.Processing, history.NewStatus);
        Assert.Equal("إلغاء الطلب", history.Reason);
    }

    [Fact]
    public void Release_hook_is_a_no_op_for_legacy_orders_without_allocations()
    {
        var order = NewOrderWithItem();

        var result = order.ReleaseOpenAllocations(new OrderOperation(3, "النظام", Now), "إلغاء الطلب");

        Assert.True(result.IsSuccess);
        Assert.Empty(order.StatusHistory);
        Assert.Empty(order.Allocations);
    }

    [Fact]
    public void Readiness_ignores_stock_allocations()
    {
        var order = new Order(1, "ORD-READY");
        order.UpdateHeader(1, OrderSource.Online, DeliveryMethod.DeliveryMan, null, null, null);
        var item = order.AddOrderItem(1, "قفاز", 10m, 2, 0m, null, null, []).Value;
        order.AddAllocation(
            OrderInventoryAllocation.Create(1, AllocationSourceKind.Warehouse, 2, warehouseId: 4).Value,
            item);

        Assert.Equal("يجب تحديد المندوب.", order.DeliveryReadinessError());
    }

    [Fact]
    public void Change_quantity_never_goes_below_the_issued_quantity()
    {
        var allocation = OrderInventoryAllocation.Create(1, AllocationSourceKind.Warehouse, 5, warehouseId: 1).Value;

        Assert.True(allocation.ChangeQuantity(3).IsSuccess);
        Assert.Equal(3, allocation.Qty);
        Assert.True(allocation.Release().IsSuccess);
        Assert.False(allocation.ChangeQuantity(4).IsSuccess);
        Assert.Equal(OrderAllocationStatus.Released, allocation.Status);
    }

    private static Order NewOrderWithItem()
    {
        var order = new Order(1, "ORD-UNIT");
        order.UpdateHeader(1, OrderSource.Online, DeliveryMethod.Pickup, null, null, null);
        order.AddOrderItem(9, "قفاز طبي", 10m, 2, 0m, null, null, []);
        return order;
    }

    private static OrderInput.Item Item(
        int quantity,
        OrderInput.Allocation[]? allocations = null,
        int? supplierId = null,
        string? supplierName = null,
        int? itemId = null) =>
        new(9, "قفاز طبي", quantity, 10m, 0m, supplierName!, supplierId, [],
            itemId, allocations);

    private static OrderInput.Allocation Warehouse(int warehouseId, int quantity) =>
        new(AllocationSourceKind.Warehouse, quantity, warehouseId);

    private static OrderInput.Allocation SupplierAllocation(int supplierId, int quantity) =>
        new(AllocationSourceKind.Supplier, quantity, null, supplierId);

    private static OrderInput.Allocation[] Allocations(params OrderInput.Allocation[] allocations) =>
        allocations;
}
