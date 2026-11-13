using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.ConsolidatedBalances;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class ConsolidatedInventoryTests
{
    [Fact]
    public async Task Products_are_summed_across_every_warehouse_in_scope()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 10);
        db.SeedReservation(db.MainWarehouse, db.Product, 4);
        db.StockIn(db.SecondWarehouse, db.Product, 6);
        db.StockIn(db.MainWarehouse, db.SecondProduct, 3);
        SeedMovement(db, db.MainWarehouse, db.Product, new DateTime(2026, 11, 12, 9, 10, 0, DateTimeKind.Utc));
        SeedMovement(db, db.SecondWarehouse, db.Product, new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc));
        SeedMovement(db, db.MainWarehouse, db.SecondProduct, new DateTime(2026, 11, 12, 9, 20, 0, DateTimeKind.Utc));

        var result = await BalancesHandler(db, db.Sales.Id, AllowApprove())
            .Handle(new GetConsolidatedInventoryBalancesQuery(PageSize: 100), default);

        Assert.True(result.IsSuccess);
        var balances = result.Value;
        Assert.False(balances.IsKeeperScoped);
        Assert.Equal("كل المخازن", balances.ScopeLabel);
        Assert.Equal(19, balances.Totals.StockOnHand);
        Assert.Equal(4, balances.Totals.ReservedOnHand);
        Assert.Equal(15, balances.Totals.Available);
        Assert.Equal(2, balances.Totals.ProductCount);
        Assert.Equal(2, balances.Totals.WithStockCount);
        Assert.Equal(2, balances.TotalCount);
        Assert.Equal(2, balances.Rows.Count);

        var glove = balances.Rows.Single(row => row.ProductId == db.Product.Id);
        Assert.Equal(16, glove.StockOnHand);
        Assert.Equal(4, glove.ReservedOnHand);
        Assert.Equal(12, glove.Available);
        Assert.Equal(2, glove.WarehouseCount);
        Assert.Equal(StockStatusFilter.PartiallyReserved, glove.Status);
        Assert.Equal(new DateTime(2026, 11, 12, 9, 30, 0), glove.LastMovementAtUtc);

        var mask = balances.Rows.Single(row => row.ProductId == db.SecondProduct.Id);
        Assert.Equal(3, mask.StockOnHand);
        Assert.Equal(1, mask.WarehouseCount);
        Assert.Equal(StockStatusFilter.Available, mask.Status);
        Assert.Equal(new DateTime(2026, 11, 12, 9, 20, 0), mask.LastMovementAtUtc);

        var category = Assert.Single(balances.Categories);
        Assert.Equal(db.Category.Id, category.Id);
        Assert.Equal(2, category.ProductCount);

        var mainTotals = await db.Inventory.GetWarehouseStockTotalsAsync(db.MainWarehouse.Id);
        var secondTotals = await db.Inventory.GetWarehouseStockTotalsAsync(db.SecondWarehouse.Id);
        Assert.Equal(mainTotals.StockOnHand + secondTotals.StockOnHand, balances.Totals.StockOnHand);
        Assert.Equal(mainTotals.ReservedOnHand + secondTotals.ReservedOnHand, balances.Totals.ReservedOnHand);
        Assert.Equal(mainTotals.DamagedQty + secondTotals.DamagedQty, balances.Totals.DamagedQty);
    }

    [Fact]
    public async Task A_keeper_only_sees_the_bound_warehouse()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 5);
        db.StockIn(db.SecondWarehouse, db.Product, 7);
        db.StockIn(db.SecondWarehouse, db.SecondProduct, 2);

        var result = await BalancesHandler(db, db.Keeper.Id, AllowWrite())
            .Handle(new GetConsolidatedInventoryBalancesQuery(PageSize: 100), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsKeeperScoped);
        Assert.Equal("مخزن المخزن الرئيسي", result.Value.ScopeLabel);
        Assert.Equal(5, result.Value.Totals.StockOnHand);
        Assert.Equal(1, result.Value.Totals.ProductCount);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(db.Product.Id, Assert.Single(result.Value.Rows).ProductId);
        Assert.Equal(1, Assert.Single(result.Value.Rows).WarehouseCount);
        Assert.Equal(db.Category.Id, Assert.Single(result.Value.Categories).Id);
        Assert.Equal(1, result.Value.Categories[0].ProductCount);
    }

    [Fact]
    public async Task Distribution_lists_the_scoped_warehouses_including_zero_rows()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.MainWarehouse, db.Product, 10);
        db.SeedReservation(db.MainWarehouse, db.Product, 4);
        db.Context.StockBalances.Add(StockBalance.Create(db.SecondWarehouse.Id, db.Product.Id));
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var result = await DistributionHandler(db, db.Sales.Id, AllowApprove())
            .Handle(new GetProductWarehouseDistributionQuery(db.Product.Id), default);

        Assert.True(result.IsSuccess);
        var distribution = result.Value;
        Assert.False(distribution.IsKeeperScoped);
        Assert.Equal("قفاز طبي", distribution.Product.ProductName);
        Assert.Equal("GLV-001", distribution.Product.ProductCode);
        Assert.Equal("مستلزمات", distribution.Product.CategoryName);
        Assert.Equal(10, distribution.Totals.StockOnHand);
        Assert.Equal(4, distribution.Totals.ReservedOnHand);
        Assert.Equal(6, distribution.Totals.Available);
        Assert.Equal(1, distribution.Totals.ProductCount);

        Assert.Equal(2, distribution.Warehouses.Count);
        Assert.Equal(db.SecondWarehouse.Id, distribution.Warehouses[0].WarehouseId);
        Assert.Equal(0, distribution.Warehouses[0].StockOnHand);
        Assert.Equal(StockStatusFilter.OutOfStock,
            InventoryPresentation.StockStatusOf(
                distribution.Warehouses[0].StockOnHand,
                distribution.Warehouses[0].ReservedOnHand));
        Assert.Equal(db.MainWarehouse.Id, distribution.Warehouses[1].WarehouseId);
        Assert.Equal(10, distribution.Warehouses[1].StockOnHand);
        Assert.Equal(6, distribution.Warehouses[1].Available);
        Assert.Equal(
            distribution.Totals.StockOnHand,
            distribution.Warehouses.Sum(warehouse => warehouse.StockOnHand));
    }

    [Fact]
    public async Task Keeper_distribution_never_leaves_the_bound_warehouse()
    {
        using var db = new OrderAllocationTestDb();
        db.StockIn(db.SecondWarehouse, db.Product, 4);

        var result = await DistributionHandler(db, db.Keeper.Id, AllowWrite())
            .Handle(new GetProductWarehouseDistributionQuery(db.Product.Id), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
    }

    [Fact]
    public async Task Filters_never_change_the_scope_totals()
    {
        using var db = new OrderAllocationTestDb();
        (Product fullyReserved, Product zero) = SeedFourProducts(db);

        var searched = await BalancesHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetConsolidatedInventoryBalancesQuery(Search: "قفاز"), default);

        Assert.True(searched.IsSuccess);
        Assert.Equal(1, searched.Value.TotalCount);
        Assert.Equal(db.Product.Id, Assert.Single(searched.Value.Rows).ProductId);
        Assert.False(searched.Value.HasFilteredView);
        Assert.Equal(4, searched.Value.Totals.ProductCount);
        Assert.Equal(4, searched.Value.FilteredTotals.ProductCount);

        var byStatus = await BalancesHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetConsolidatedInventoryBalancesQuery(Status: StockStatusFilter.FullyReserved), default);

        Assert.True(byStatus.IsSuccess);
        Assert.True(byStatus.Value.HasFilteredView);
        Assert.Equal(1, byStatus.Value.TotalCount);
        Assert.Equal(fullyReserved.Id, Assert.Single(byStatus.Value.Rows).ProductId);
        Assert.Equal(1, byStatus.Value.FilteredTotals.ProductCount);
        Assert.Equal(2, byStatus.Value.FilteredTotals.StockOnHand);
        Assert.Equal(4, byStatus.Value.Totals.ProductCount);

        var byCategory = await BalancesHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetConsolidatedInventoryBalancesQuery(CategoryId: zero.Category.Id), default);

        Assert.True(byCategory.IsSuccess);
        Assert.Equal(1, byCategory.Value.TotalCount);
        Assert.Equal(zero.Id, Assert.Single(byCategory.Value.Rows).ProductId);
        Assert.Equal(1, byCategory.Value.FilteredTotals.ProductCount);

        var hidden = await BalancesHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetConsolidatedInventoryBalancesQuery(HideZero: true), default);

        Assert.True(hidden.IsSuccess);
        Assert.Equal(3, hidden.Value.TotalCount);
        Assert.DoesNotContain(hidden.Value.Rows, row => row.ProductId == zero.Id);
        Assert.Equal(4, hidden.Value.Totals.ProductCount);
        Assert.Equal(4, hidden.Value.FilteredTotals.ProductCount);
    }

    [Fact]
    public async Task Paging_clamps_and_sorts_on_the_aggregated_values()
    {
        using var db = new OrderAllocationTestDb();
        (_, Product zero) = SeedFourProducts(db);

        var firstPage = await BalancesHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetConsolidatedInventoryBalancesQuery(
                Sort: StockSortColumn.Available,
                Descending: true,
                Page: 1,
                PageSize: 2), default);

        Assert.True(firstPage.IsSuccess);
        Assert.Equal(2, firstPage.Value.Rows.Count);
        Assert.Equal(4, firstPage.Value.TotalCount);
        Assert.Equal(db.Product.Id, firstPage.Value.Rows[0].ProductId);
        Assert.Equal(6, firstPage.Value.Rows[0].Available);
        Assert.Equal(db.SecondProduct.Id, firstPage.Value.Rows[1].ProductId);
        Assert.Equal(1, firstPage.Value.Page);
        Assert.Equal(2, firstPage.Value.PageSize);

        var clamped = await BalancesHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetConsolidatedInventoryBalancesQuery(Page: 0, PageSize: 500), default);

        Assert.True(clamped.IsSuccess);
        Assert.Equal(1, clamped.Value.Page);
        Assert.Equal(StockPaging.MaxPageSize, clamped.Value.PageSize);

        var byName = await BalancesHandler(db, db.Sales.Id, AllowApprove()).Handle(
            new GetConsolidatedInventoryBalancesQuery(
                Sort: StockSortColumn.ProductName,
                Descending: false), default);

        Assert.True(byName.IsSuccess);
        Assert.Equal(
            new[] { "قفاز طبي", "كمامة طبية", "محلول طبي", "مقص طبي" },
            byName.Value.Rows.Select(row => row.ProductName));
        Assert.Contains(byName.Value.Rows, row => row.ProductId == zero.Id);
    }

    [Fact]
    public async Task Anonymous_callers_are_unauthorized()
    {
        using var db = new OrderAllocationTestDb();

        var result = await BalancesHandler(db, null, AllowApprove())
            .Handle(new GetConsolidatedInventoryBalancesQuery(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.ErrorCode);
    }

    private static (Product FullyReserved, Product Zero) SeedFourProducts(OrderAllocationTestDb db)
    {
        db.StockIn(db.MainWarehouse, db.Product, 10);
        db.SeedReservation(db.MainWarehouse, db.Product, 4);
        db.StockIn(db.SecondWarehouse, db.SecondProduct, 5);

        db.Context.Attach(db.Category);
        var fullyReserved = new Product("محلول طبي", "SOL-001", db.Category, new Money(3m));
        db.Context.Products.Add(fullyReserved);
        var toolsCategory = new Category("أدوات");
        db.Context.Categories.Add(toolsCategory);
        var zero = new Product("مقص طبي", "SCN-001", toolsCategory, new Money(7m));
        db.Context.Products.Add(zero);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        db.StockIn(db.MainWarehouse, fullyReserved, 2);
        db.SeedReservation(db.MainWarehouse, fullyReserved, 2);
        db.Context.StockBalances.Add(StockBalance.Create(db.SecondWarehouse.Id, zero.Id));
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        return (fullyReserved, zero);
    }

    private static void SeedMovement(
        OrderAllocationTestDb db,
        Warehouse warehouse,
        Product product,
        DateTime effectiveAtUtc)
    {
        var movement = InventoryMovement.Create(
            warehouse.Id,
            product.Id,
            1,
            InventoryDirection.In,
            InventoryMovementType.StockIn,
            effectiveAtUtc,
            db.Keeper.Id,
            "توريد تجريبي",
            nowUtc: new DateTime(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc));
        db.Context.InventoryMovements.Add(movement);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static GetConsolidatedInventoryBalancesQueryHandler BalancesHandler(
        OrderAllocationTestDb db,
        int? userId,
        IAuthorizationService authorization) =>
        new(db.Warehouses, db.Inventory, authorization, new TestCurrentUser(userId));

    private static GetProductWarehouseDistributionQueryHandler DistributionHandler(
        OrderAllocationTestDb db,
        int? userId,
        IAuthorizationService authorization) =>
        new(db.Warehouses, db.Inventory, authorization, new TestCurrentUser(userId));

    private static IAuthorizationService AllowApprove() =>
        new StubPermissions((_, level) => level is PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.Approve);

    private static IAuthorizationService AllowWrite() =>
        new StubPermissions((_, level) => level is PermissionLevel.Read or PermissionLevel.Write);
}
