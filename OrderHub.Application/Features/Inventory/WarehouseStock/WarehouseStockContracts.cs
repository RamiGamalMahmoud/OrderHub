using OrderHub.Application.Features.Inventory.Presentation;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.WarehouseStock;

public sealed record AccessibleWarehouseItem(
    int Id,
    string Name,
    string KeeperDisplayName,
    bool IsActive,
    string Location,
    string PhoneNumber,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty,
    int ProductCount,
    int WithStockCount,
    DateTime? LastMovementAtUtc)
{
    public int Available => StockOnHand - ReservedOnHand;
}

public sealed record AccessibleWarehousesResult(
    string ScopeLabel,
    bool IsKeeper,
    bool CanRecordStockIn,
    IReadOnlyList<AccessibleWarehouseItem> Warehouses);

public sealed record StockCategoryOption(int Id, string Name, int ProductCount);

public sealed record WarehouseStockTotals(
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty,
    int ProductCount,
    int WithStockCount)
{
    public int Available => StockOnHand - ReservedOnHand;
}

public sealed record WarehouseStockHeader(
    int Id,
    string Name,
    string KeeperDisplayName,
    bool IsActive,
    string CityName,
    string Street,
    string PhoneNumber);

public sealed record WarehouseStockRow(
    int ProductId,
    string ProductName,
    string ProductCode,
    int CategoryId,
    string CategoryName,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty,
    DateTime? LastMovementAtUtc)
{
    public int Available => StockOnHand - ReservedOnHand;

    public StockStatusFilter Status => InventoryPresentation.StockStatusOf(StockOnHand, ReservedOnHand);
}

public sealed record WarehouseStockResult(
    WarehouseStockHeader Header,
    WarehouseStockTotals Totals,
    WarehouseStockTotals FilteredTotals,
    bool HasFilteredView,
    IReadOnlyList<WarehouseStockRow> Rows,
    IReadOnlyList<StockCategoryOption> Categories,
    int TotalCount,
    int WithStockCount,
    int Page,
    int PageSize,
    bool IsKeeperScoped,
    bool CanRecordStockIn);

/// <summary>Projection returned by the repository; the handler maps it to the public contract.</summary>
public sealed record InventoryWarehouseSummaryRow(
    int Id,
    string Name,
    string KeeperDisplayName,
    bool IsActive,
    string CityName,
    string Street,
    string PhoneNumber,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty,
    int ProductCount,
    int WithStockCount,
    DateTime? LastMovementAtUtc);

public sealed record InventoryWarehouseHeaderRow(
    int Id,
    string Name,
    int KeeperUserId,
    string KeeperDisplayName,
    bool IsActive,
    string CityName,
    string Street,
    string PhoneNumber);

public sealed record WarehouseStockQueryFilter(
    int WarehouseId,
    string SearchTerm,
    int? CategoryId,
    StockStatusFilter Status,
    bool HideZero,
    StockSortColumn Sort,
    bool Descending,
    int Skip,
    int Take);

public sealed record WarehouseStockProjectionRow(
    int ProductId,
    string ProductName,
    string ProductCode,
    int CategoryId,
    string CategoryName,
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty,
    DateTime? LastMovementAtUtc);

public sealed record WarehouseStockTotalsRow(
    int StockOnHand,
    int ReservedOnHand,
    int DamagedQty,
    int ProductCount,
    int WithStockCount);
