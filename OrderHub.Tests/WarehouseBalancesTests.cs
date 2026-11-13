using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class WarehouseBalancesTests
{
    [Fact]
    public async Task Accessible_warehouses_are_all_for_a_manager_and_include_the_inactive_ones()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 5);
        db.StockIn(db.SecondWarehouse, db.SecondProduct, 3);

        var result = await AccessibleHandler(db, db.Sales.Id, AllowApprove())
            .Handle(new GetAccessibleWarehousesQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsKeeper);
        Assert.Equal("كل المخازن", result.Value.ScopeLabel);
        Assert.Equal(3, result.Value.Warehouses.Count);
        var main = result.Value.Warehouses.Single(item => item.Id == db.MainWarehouse.Id);
        Assert.Equal(5, main.StockOnHand);
        Assert.Equal(1, main.ProductCount);
        Assert.Equal(1, main.WithStockCount);
        Assert.False(result.Value.Warehouses.Single(item => item.Id == db.InactiveWarehouse.Id).IsActive);
        Assert.False(result.Value.CanRecordStockIn);
    }

    [Fact]
    public async Task Accessible_warehouses_are_limited_to_the_keepers_binding()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.SecondWarehouse, db.Product, 3);

        var result = await AccessibleHandler(db, db.Keeper.Id, AllowWrite())
            .Handle(new GetAccessibleWarehousesQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsKeeper);
        Assert.True(result.Value.CanRecordStockIn);
        Assert.Equal("مخزن المخزن الرئيسي", result.Value.ScopeLabel);
        Assert.Equal(db.MainWarehouse.Id, Assert.Single(result.Value.Warehouses).Id);
        Assert.Equal(0, result.Value.Warehouses[0].StockOnHand);
    }

    [Fact]
    public async Task Stock_page_uses_the_available_arithmetic_for_status_totals_and_counts()
    {
        using var db = new OrderAllocationTestDb();
        (Product outOfStock, Product fullyReserved, Product available) = SeedFourProducts(db);

        var result = await StockHandler(db, db.Sales.Id, AllowApprove())
            .Handle(new GetWarehouseStockQuery(db.MainWarehouse.Id, PageSize: 100), default);

        Assert.True(result.IsSuccess);
        var stock = result.Value;
        Assert.Equal(db.MainWarehouse.Id, stock.Header.Id);
        Assert.Equal(17, stock.Totals.StockOnHand);
        Assert.Equal(6, stock.Totals.ReservedOnHand);
        Assert.Equal(11, stock.Totals.Available);
        Assert.Equal(4, stock.Totals.ProductCount);
        Assert.Equal(3, stock.Totals.WithStockCount);
        Assert.Equal(4, stock.TotalCount);
        Assert.Equal(3, stock.WithStockCount);

        Assert.Equal(StockStatusFilter.PartiallyReserved, stock.Rows.Single(row => row.ProductId == db.Product.Id).Status);
        Assert.Equal(StockStatusFilter.OutOfStock, stock.Rows.Single(row => row.ProductId == outOfStock.Id).Status);
        Assert.Equal(StockStatusFilter.FullyReserved, stock.Rows.Single(row => row.ProductId == fullyReserved.Id).Status);
        Assert.Equal(StockStatusFilter.Available, stock.Rows.Single(row => row.ProductId == available.Id).Status);
        Assert.Equal(0, stock.Rows.Single(row => row.ProductId == fullyReserved.Id).Available);

        Assert.Equal(2, stock.Categories.Count);
        Assert.Equal(3, stock.Categories.Single(category => category.Id == db.Category.Id).ProductCount);
        Assert.False(stock.HasFilteredView);
        Assert.False(stock.CanRecordStockIn);
    }

    [Fact]
    public async Task Search_hides_zero_rows_and_filters_never_change_the_full_totals()
    {
        using var db = new OrderAllocationTestDb();
        SeedFourProducts(db);

        var searched = await StockHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(db.MainWarehouse.Id, Search: "قفاز"), default);

        Assert.True(searched.IsSuccess);
        Assert.Equal(1, searched.Value.TotalCount);
        Assert.Equal(db.Product.Id, Assert.Single(searched.Value.Rows).ProductId);
        Assert.Equal(4, searched.Value.Totals.ProductCount);
        Assert.Equal(4, searched.Value.FilteredTotals.ProductCount);
        Assert.False(searched.Value.HasFilteredView);

        var hidden = await StockHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(db.MainWarehouse.Id, HideZero: true), default);

        Assert.True(hidden.IsSuccess);
        Assert.Equal(3, hidden.Value.TotalCount);
        Assert.DoesNotContain(hidden.Value.Rows, row => row.ProductId == db.SecondProduct.Id);
        Assert.Equal(4, hidden.Value.Totals.ProductCount);
        Assert.Equal(4, hidden.Value.FilteredTotals.ProductCount);
    }

    [Fact]
    public async Task Status_and_category_filters_produce_filtered_totals_with_a_hint()
    {
        using var db = new OrderAllocationTestDb();
        (_, _, Product toolsProduct) = SeedFourProducts(db);

        var byStatus = await StockHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(
                db.MainWarehouse.Id,
                Status: StockStatusFilter.Available), default);

        Assert.True(byStatus.IsSuccess);
        Assert.True(byStatus.Value.HasFilteredView);
        Assert.Equal(1, byStatus.Value.FilteredTotals.ProductCount);
        Assert.Equal(5, byStatus.Value.FilteredTotals.StockOnHand);
        Assert.Equal(4, byStatus.Value.Totals.ProductCount);
        Assert.Equal(toolsProduct.Id, Assert.Single(byStatus.Value.Rows).ProductId);

        var byCategory = await StockHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(
                db.MainWarehouse.Id,
                CategoryId: toolsProduct.Category.Id), default);

        Assert.True(byCategory.IsSuccess);
        Assert.Equal(1, byCategory.Value.FilteredTotals.ProductCount);
        Assert.Equal(1, byCategory.Value.TotalCount);
    }

    [Fact]
    public async Task Paging_is_deterministic_clamped_and_sorted_server_side()
    {
        using var db = new OrderAllocationTestDb();
        SeedFourProducts(db);

        var firstPage = await StockHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(
                db.MainWarehouse.Id,
                HideZero: true,
                Sort: StockSortColumn.Available,
                Descending: true,
                Page: 1,
                PageSize: 2), default);

        Assert.True(firstPage.IsSuccess);
        Assert.Equal(2, firstPage.Value.Rows.Count);
        Assert.Equal(3, firstPage.Value.TotalCount);
        Assert.Equal(db.Product.Id, firstPage.Value.Rows[0].ProductId);
        Assert.Equal(6, firstPage.Value.Rows[0].Available);
        Assert.Equal(1, firstPage.Value.Page);
        Assert.Equal(2, firstPage.Value.PageSize);

        var secondPage = await StockHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(
                db.MainWarehouse.Id,
                HideZero: true,
                Sort: StockSortColumn.Available,
                Descending: true,
                Page: 2,
                PageSize: 2), default);

        Assert.True(secondPage.IsSuccess);
        Assert.Single(secondPage.Value.Rows);

        var clamped = await StockHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(db.MainWarehouse.Id, Page: 0, PageSize: 500), default);

        Assert.True(clamped.IsSuccess);
        Assert.Equal(1, clamped.Value.Page);
        Assert.Equal(StockPaging.MaxPageSize, clamped.Value.PageSize);

        var byName = await StockHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(
                db.MainWarehouse.Id,
                Sort: StockSortColumn.ProductName,
                Descending: false), default);

        Assert.True(byName.IsSuccess);
        var names = byName.Value.Rows.Select(row => row.ProductName).ToList();
        Assert.Equal(names.OrderBy(name => name, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task Stock_page_enforces_the_scope_and_the_inactive_rule()
    {
        using var db = new OrderAllocationTestDb();
        SeedFourProducts(db);

        var foreign = await StockHandler(db, db.Keeper.Id, AllowWrite()).Handle(
            new GetWarehouseStockQuery(db.SecondWarehouse.Id), default);
        Assert.False(foreign.IsSuccess);
        Assert.Equal("Forbidden", foreign.ErrorCode);

        var missing = await StockHandler(db, db.Keeper.Id, AllowWrite()).Handle(
            new GetWarehouseStockQuery(9999), default);
        Assert.False(missing.IsSuccess);
        Assert.Equal("NotFound", missing.ErrorCode);

        var anonymous = await StockHandler(db, null, AllowWrite()).Handle(
            new GetWarehouseStockQuery(db.MainWarehouse.Id), default);
        Assert.False(anonymous.IsSuccess);
        Assert.Equal("Unauthorized", anonymous.ErrorCode);

        var inactiveKeeper = await StockHandler(db, db.InactiveWarehouse.KeeperUserId, AllowWrite()).Handle(
            new GetWarehouseStockQuery(db.InactiveWarehouse.Id), default);
        Assert.True(inactiveKeeper.IsSuccess);
        Assert.False(inactiveKeeper.Value.Header.IsActive);
        Assert.False(inactiveKeeper.Value.CanRecordStockIn);

        var keeper = await StockHandler(db, db.Keeper.Id, AllowWrite()).Handle(
            new GetWarehouseStockQuery(db.MainWarehouse.Id), default);
        Assert.True(keeper.IsSuccess);
        Assert.True(keeper.Value.CanRecordStockIn);
    }

    [Fact]
    public async Task Keeper_with_manager_approval_follows_the_manager_scope()
    {
        using var db = new OrderAllocationTestDb();
        SeedFourProducts(db);

        var result = await StockHandler(db, db.Keeper.Id, AllowApprove()).Handle(
            new GetWarehouseStockQuery(db.SecondWarehouse.Id), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.CanRecordStockIn);
        Assert.Equal(db.SecondWarehouse.Id, result.Value.Header.Id);
    }

    private static (Product OutOfStock, Product FullyReserved, Product Available) SeedFourProducts(
        OrderAllocationTestDb db)
    {
        db.StockIn(db.MainWarehouse, db.Product, 10);
        db.SeedReservation(db.MainWarehouse, db.Product, 4);
        db.Context.StockBalances.Add(StockBalance.Create(db.MainWarehouse.Id, db.SecondProduct.Id));
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        db.Context.Attach(db.Category);
        var fullyReserved = new Product("محلول طبي", "SOL-001", db.Category, new Money(3m));
        db.Context.Products.Add(fullyReserved);
        var toolsCategory = new Category("أدوات");
        db.Context.Categories.Add(toolsCategory);
        var available = new Product("مقص طبي", "SCN-001", toolsCategory, new Money(7m));
        db.Context.Products.Add(available);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        db.StockIn(db.MainWarehouse, fullyReserved, 2);
        db.SeedReservation(db.MainWarehouse, fullyReserved, 2);
        db.StockIn(db.MainWarehouse, available, 5);

        return (db.SecondProduct, fullyReserved, available);
    }

    private static GetAccessibleWarehousesQueryHandler AccessibleHandler(
        OrderAllocationTestDb db,
        int? userId,
        IAuthorizationService authorization) =>
        new(db.Warehouses, authorization, new TestCurrentUser(userId));

    private static GetWarehouseStockQueryHandler StockHandler(
        OrderAllocationTestDb db,
        int? userId,
        IAuthorizationService authorization) =>
        new(db.Warehouses, db.Inventory, authorization, new TestCurrentUser(userId));

    private static IAuthorizationService AllowApprove() =>
        new StubPermissions((_, level) => level is PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.Approve);

    private static IAuthorizationService AllowWrite() =>
        new StubPermissions((_, level) => level is PermissionLevel.Read or PermissionLevel.Write);
}

internal sealed class StubPermissions(Func<PermissionFeature, PermissionLevel, bool> check) : IAuthorizationService
{
    public Task<bool> IsAllowedAsync(
        PermissionFeature feature,
        PermissionLevel requiredLevel,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(check(feature, requiredLevel));

    public Task<bool> IsHigherManagerAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(check(PermissionFeature.Inventory, PermissionLevel.Approve));
}
