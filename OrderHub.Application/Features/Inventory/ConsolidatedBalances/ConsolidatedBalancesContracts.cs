using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.ConsolidatedBalances;

/// <summary>Aggregated row of one product across every warehouse inside the caller's scope.</summary>
public sealed record ConsolidatedStockRow(
    int ProductId,
    string ProductName,
    string ProductCode,
    int CategoryId,
    string CategoryName,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty,
    int WarehouseCount,
    DateTime? LastMovementAtUtc)
{
    public int Available => StockOnHand - ReservedOnHand;

    public StockStatusFilter Status => InventoryPresentation.StockStatusOf(StockOnHand, ReservedOnHand);
}

public sealed record ConsolidatedBalancesResult(
    string ScopeLabel,
    WarehouseStockTotals Totals,
    WarehouseStockTotals FilteredTotals,
    bool HasFilteredView,
    IReadOnlyList<ConsolidatedStockRow> Rows,
    IReadOnlyList<StockCategoryOption> Categories,
    int TotalCount,
    int WithStockCount,
    int Page,
    int PageSize,
    bool IsKeeperScoped);

public sealed record ConsolidatedStockQueryFilter(
    IReadOnlyCollection<int> WarehouseIds,
    string SearchTerm,
    int? CategoryId,
    StockStatusFilter Status,
    bool HideZero,
    StockSortColumn Sort,
    bool Descending,
    int Skip,
    int Take);

/// <summary>Per-product aggregate produced by the repository before product details are joined.</summary>
public sealed record ConsolidatedStockProjectionRow(
    int ProductId,
    string ProductName,
    string ProductCode,
    int CategoryId,
    string CategoryName,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty,
    int WarehouseCount,
    DateTime? LastMovementAtUtc);

public sealed record ProductDistributionProduct(
    int ProductId,
    string ProductName,
    string ProductCode,
    string CategoryName,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty)
{
    public int Available => StockOnHand - ReservedOnHand;
}

public sealed record ProductDistributionWarehouse(
    int WarehouseId,
    string WarehouseName,
    bool IsActive,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty)
{
    public int Available => StockOnHand - ReservedOnHand;
}

public sealed record ProductWarehouseDistributionResult(
    ProductDistributionProduct Product,
    WarehouseStockTotals Totals,
    IReadOnlyList<ProductDistributionWarehouse> Warehouses,
    bool IsKeeperScoped);

public sealed record ProductDistributionProjectionRow(
    int ProductId,
    string ProductName,
    string ProductCode,
    string CategoryName,
    int WarehouseId,
    string WarehouseName,
    bool IsActive,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty);
