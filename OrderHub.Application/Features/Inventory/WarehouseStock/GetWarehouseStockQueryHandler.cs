using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.WarehouseStock;

internal sealed class GetWarehouseStockQueryHandler(
    IWarehouseRepository warehouses,
    IInventoryRepository inventory,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetWarehouseStockQuery, Result<WarehouseStockResult>>
{
    public async Task<Result<WarehouseStockResult>> Handle(
        GetWarehouseStockQuery request,
        CancellationToken cancellationToken)
    {
        Result<InventoryScope> scopeResult = await InventoryScope.ResolveAsync(
            currentUser,
            authorization,
            warehouses,
            cancellationToken);
        if (!scopeResult.IsSuccess)
            return Result<WarehouseStockResult>.Failure(scopeResult.ErrorMessage, scopeResult.ErrorCode);

        InventoryScope scope = scopeResult.Value;
        Result<InventoryWarehouseHeaderRow> warehouseResult = await InventoryScope.ResolveWarehouseAsync(
            scope,
            warehouses,
            request.WarehouseId,
            cancellationToken);
        if (!warehouseResult.IsSuccess)
            return Result<WarehouseStockResult>.Failure(warehouseResult.ErrorMessage, warehouseResult.ErrorCode);

        InventoryWarehouseHeaderRow warehouse = warehouseResult.Value;
        (int page, int pageSize) = StockPaging.Normalize(request.Page, request.PageSize);
        int skip = (page - 1) * pageSize;
        string search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        var filter = new WarehouseStockQueryFilter(
            warehouse.Id,
            search,
            request.CategoryId,
            request.Status,
            request.HideZero,
            request.Sort,
            request.Descending,
            skip,
            pageSize);

        WarehouseStockTotalsRow fullTotals = await inventory
            .GetWarehouseStockTotalsAsync(warehouse.Id, cancellationToken);
        WarehouseStockTotalsRow filteredTotals = await inventory
            .GetWarehouseStockFilteredTotalsAsync(filter, cancellationToken);
        int totalCount = await inventory.CountWarehouseStockAsync(filter, cancellationToken);
        int withStockCount = await inventory.CountWarehouseStockWithStockAsync(filter, cancellationToken);
        IReadOnlyList<WarehouseStockProjectionRow> rows = await inventory
            .GetWarehouseStockPageAsync(filter, cancellationToken);
        IReadOnlyList<StockCategoryOption> categories = await inventory
            .GetWarehouseStockCategoriesAsync(warehouse.Id, cancellationToken);

        bool canRecordStockIn = warehouse.IsActive &&
            scope.IsKeeper &&
            await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Write,
                cancellationToken);

        return Result<WarehouseStockResult>.Success(new WarehouseStockResult(
            new WarehouseStockHeader(
                warehouse.Id,
                warehouse.Name,
                warehouse.KeeperDisplayName,
                warehouse.IsActive,
                warehouse.CityName,
                warehouse.Street,
                warehouse.PhoneNumber),
            ToTotals(fullTotals),
            ToTotals(filteredTotals),
            request.CategoryId.HasValue || request.Status != StockStatusFilter.All,
            rows.Select(row => new WarehouseStockRow(
                row.ProductId,
                row.ProductName,
                row.ProductCode,
                row.CategoryId,
                row.CategoryName,
                row.StockOnHand,
                row.ReservedOnHand,
                row.DamagedQty,
                row.LastMovementAtUtc)).ToList(),
            categories,
            totalCount,
            withStockCount,
            page,
            pageSize,
            scope.IsKeeper,
            canRecordStockIn));
    }

    private static WarehouseStockTotals ToTotals(WarehouseStockTotalsRow row) =>
        row is null
            ? new WarehouseStockTotals(0, 0, 0, 0, 0)
            : new WarehouseStockTotals(
                row.StockOnHand,
                row.ReservedOnHand,
                row.DamagedQty,
                row.ProductCount,
                row.WithStockCount);
}
