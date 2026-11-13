using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.ConsolidatedBalances;

internal sealed class GetConsolidatedInventoryBalancesQueryHandler(
    IWarehouseRepository warehouses,
    IInventoryRepository inventory,
    IAuthorizationService authorization,
    ICurrentUser currentUser)
    : IRequestHandler<GetConsolidatedInventoryBalancesQuery, Result<ConsolidatedBalancesResult>>
{
    public async Task<Result<ConsolidatedBalancesResult>> Handle(
        GetConsolidatedInventoryBalancesQuery request,
        CancellationToken cancellationToken)
    {
        Result<InventoryScope> scopeResult = await InventoryScope.ResolveAsync(
            currentUser,
            authorization,
            warehouses,
            cancellationToken);
        if (!scopeResult.IsSuccess)
            return Result<ConsolidatedBalancesResult>.Failure(scopeResult.ErrorMessage, scopeResult.ErrorCode);

        InventoryScope scope = scopeResult.Value;
        IReadOnlyCollection<int> scopeIds = scope.IsUnrestricted
            ? null
            : [scope.KeeperWarehouseId];

        (int page, int pageSize) = StockPaging.Normalize(request.Page, request.PageSize);
        int skip = (page - 1) * pageSize;
        string search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        var filter = new ConsolidatedStockQueryFilter(
            scopeIds,
            search,
            request.CategoryId,
            request.Status,
            request.HideZero,
            request.Sort,
            request.Descending,
            skip,
            pageSize);

        WarehouseStockTotalsRow fullTotals = await inventory
            .GetConsolidatedStockTotalsAsync(scopeIds, cancellationToken);
        WarehouseStockTotalsRow filteredTotals = await inventory
            .GetConsolidatedStockFilteredTotalsAsync(filter, cancellationToken);
        int totalCount = await inventory.CountConsolidatedStockAsync(filter, cancellationToken);
        int withStockCount = await inventory.CountConsolidatedStockWithStockAsync(filter, cancellationToken);
        IReadOnlyList<ConsolidatedStockProjectionRow> rows = await inventory
            .GetConsolidatedStockPageAsync(filter, cancellationToken);
        IReadOnlyList<StockCategoryOption> categories = await inventory
            .GetConsolidatedStockCategoriesAsync(scopeIds, cancellationToken);

        return Result<ConsolidatedBalancesResult>.Success(new ConsolidatedBalancesResult(
            scope.ScopeLabel,
            ToTotals(fullTotals),
            ToTotals(filteredTotals),
            request.CategoryId.HasValue || request.Status != StockStatusFilter.All,
            rows.Select(row => new ConsolidatedStockRow(
                row.ProductId,
                row.ProductName,
                row.ProductCode,
                row.CategoryId,
                row.CategoryName,
                row.StockOnHand,
                row.ReservedOnHand,
                row.DamagedQty,
                row.WarehouseCount,
                row.LastMovementAtUtc)).ToList(),
            categories,
            totalCount,
            withStockCount,
            page,
            pageSize,
            scope.IsKeeper));
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
