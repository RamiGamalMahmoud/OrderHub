using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.GetStockAvailability;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Features.Inventory.WarehouseLedger;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<StockAvailabilityRow>> GetAvailabilityAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from warehouse in _dbContext.Warehouses.AsNoTracking()
            join balance in _dbContext.StockBalances.AsNoTracking()
                on warehouse.Id equals balance.WarehouseId into balances
            from balance in balances.Where(item => item.ProductId == productId).DefaultIfEmpty()
            where warehouse.IsActive
            select new
            {
                warehouse.Id,
                Name = warehouse.Name.Value,
                StockOnHand = balance == null ? 0 : balance.StockOnHand,
                ReservedOnHand = balance == null ? 0 : balance.ReservedOnHand
            }).ToListAsync(cancellationToken);

        return rows
            .Select(row => new StockAvailabilityRow(
                row.Id,
                row.Name,
                row.StockOnHand,
                row.ReservedOnHand,
                row.StockOnHand - row.ReservedOnHand))
            .OrderByDescending(row => row.Available)
            .ThenBy(row => row.WarehouseName)
            .ToList();
    }

    public async Task LockBalancesAsync(
        IReadOnlyCollection<(int WarehouseId, int ProductId)> keys,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) != true
            || _dbContext.Database.CurrentTransaction is null)
            return;

        foreach (var key in keys.Distinct()
                     .OrderBy(item => item.ProductId)
                     .ThenBy(item => item.WarehouseId))
        {
            await _dbContext.StockBalances
                .FromSqlInterpolated(
                    $"SELECT * FROM stock_balances WHERE warehouse_id = {key.WarehouseId} AND product_id = {key.ProductId} FOR UPDATE")
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }

    public Task<StockBalance> GetStockItemAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.StockBalances
            .FirstOrDefaultAsync(
                x => x.WarehouseId == warehouseId && x.ProductId == productId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<StockBalance>> GetStockAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockBalances
            .AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId)
            .OrderBy(x => x.ProductId)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockBalance> GetOrCreateStockItemAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var stockItem = await GetStockItemAsync(warehouseId, productId, cancellationToken);
        if (stockItem is not null)
            return stockItem;

        stockItem = StockBalance.Create(warehouseId, productId);
        _dbContext.StockBalances.Add(stockItem);
        return stockItem;
    }

    public void Add(InventoryMovement movement)
    {
        _dbContext.InventoryMovements.Add(movement);
    }

    public async Task<bool> TryReserveAsync(
        int warehouseId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var modified = await _dbContext.StockBalances
            .Where(x => x.WarehouseId == warehouseId
                && x.ProductId == productId
                && x.StockOnHand - x.ReservedOnHand >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ReservedOnHand, x => x.ReservedOnHand + quantity)
                    .SetProperty(x => x.Version, x => x.Version + 1)
                    .SetProperty(x => x.ModifiedAt, DateTime.UtcNow),
                cancellationToken);

        return modified == 1;
    }

    public async Task<bool> ReleaseAsync(
        int warehouseId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var modified = await _dbContext.StockBalances
            .Where(x => x.WarehouseId == warehouseId
                && x.ProductId == productId
                && x.ReservedOnHand >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ReservedOnHand, x => x.ReservedOnHand - quantity)
                    .SetProperty(x => x.Version, x => x.Version + 1)
                    .SetProperty(x => x.ModifiedAt, DateTime.UtcNow),
                cancellationToken);

        return modified == 1;
    }

    public async Task<bool> ConsumeAsync(
        int warehouseId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var modified = await _dbContext.StockBalances
            .Where(x => x.WarehouseId == warehouseId
                && x.ProductId == productId
                && x.ReservedOnHand >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.StockOnHand, x => x.StockOnHand - quantity)
                    .SetProperty(x => x.ReservedOnHand, x => x.ReservedOnHand - quantity)
                    .SetProperty(x => x.Version, x => x.Version + 1)
                    .SetProperty(x => x.ModifiedAt, DateTime.UtcNow),
                cancellationToken);

        return modified == 1;
    }

    public async Task<IReadOnlyList<InventoryMovement>> GetLedgerAsync(
        int warehouseId,
        int? productId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        return await LedgerQuery(warehouseId, productId)
            .OrderByDescending(x => x.EffectiveAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountLedgerAsync(
        int warehouseId,
        int? productId,
        CancellationToken cancellationToken = default)
    {
        return LedgerQuery(warehouseId, productId).CountAsync(cancellationToken);
    }

    public Task<bool> HasMovementsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryMovements.AnyAsync(
            movement => movement.WarehouseId == warehouseId,
            cancellationToken);

    public async Task<IReadOnlyList<WarehouseLedgerProjectionRow>> GetWarehouseLedgerPageAsync(
        int warehouseId,
        int productId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        var query =
            from movement in _dbContext.InventoryMovements.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking()
                on movement.CreatedByUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            join receipt in _dbContext.StockReceipts.AsNoTracking()
                on movement.SourceStockReceiptId equals (int?)receipt.Id into receipts
            from receipt in receipts.DefaultIfEmpty()
            join batch in _dbContext.StockIssueBatches.AsNoTracking()
                on movement.SourceIssueBatchId equals (int?)batch.Id into batches
            from batch in batches.DefaultIfEmpty()
            where movement.WarehouseId == warehouseId && movement.ProductId == productId
            select new
            {
                movement.Id,
                movement.MovementType,
                movement.Direction,
                movement.Status,
                movement.Quantity,
                movement.EffectiveAtUtc,
                movement.CreatedAtUtc,
                PerformedByName = user == null ? null : user.DisplayName,
                movement.Note,
                ReceiptReferenceNote = receipt == null ? null : receipt.ReferenceNote,
                IssueOrderNumber = batch == null
                    ? (long?)null
                    : batch.IssueOrderLine.IssueOrder.WorkRecordNumber,
                IssueOrderYear = batch == null
                    ? (int?)null
                    : batch.IssueOrderLine.IssueOrder.SequenceYear,
                IssueOrderMonth = batch == null
                    ? (int?)null
                    : batch.IssueOrderLine.IssueOrder.SequenceMonth,
                movement.ReversalOfMovementId
            };

        var rows = await query
            .OrderByDescending(row => row.EffectiveAtUtc)
            .ThenByDescending(row => row.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new WarehouseLedgerProjectionRow(
                row.Id,
                row.MovementType,
                row.Direction,
                row.Status,
                row.Quantity,
                row.EffectiveAtUtc,
                row.CreatedAtUtc,
                row.PerformedByName,
                row.Note,
                row.ReceiptReferenceNote,
                row.IssueOrderNumber,
                row.IssueOrderYear,
                row.IssueOrderMonth,
                row.ReversalOfMovementId))
            .ToList();
    }

    public Task<WarehouseProductStockRow> GetWarehouseProductStockAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockBalances
            .AsNoTracking()
            .Where(balance => balance.WarehouseId == warehouseId && balance.ProductId == productId)
            .Select(balance => new WarehouseProductStockRow(
                balance.ProductId,
                balance.Product.Name.Value,
                balance.Product.Code,
                balance.StockOnHand,
                balance.ReservedOnHand,
                balance.DamagedQty))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<WarehouseStockTotalsRow> GetWarehouseStockTotalsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockBalances
            .AsNoTracking()
            .Where(balance => balance.WarehouseId == warehouseId)
            .GroupBy(balance => 1)
            .Select(group => new WarehouseStockTotalsRow(
                group.Sum(balance => balance.StockOnHand),
                group.Sum(balance => balance.ReservedOnHand),
                group.Sum(balance => balance.DamagedQty),
                group.Count(),
                group.Count(balance => balance.StockOnHand > 0)))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<WarehouseStockTotalsRow> GetWarehouseStockFilteredTotalsAsync(
        WarehouseStockQueryFilter filter,
        CancellationToken cancellationToken = default) =>
        FilteredBalances(filter, applySearch: false, applyHideZero: false)
            .GroupBy(balance => 1)
            .Select(group => new WarehouseStockTotalsRow(
                group.Sum(balance => balance.StockOnHand),
                group.Sum(balance => balance.ReservedOnHand),
                group.Sum(balance => balance.DamagedQty),
                group.Count(),
                group.Count(balance => balance.StockOnHand > 0)))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<int> CountWarehouseStockAsync(
        WarehouseStockQueryFilter filter,
        CancellationToken cancellationToken = default) =>
        FilteredBalances(filter, applySearch: true, applyHideZero: filter.HideZero)
            .CountAsync(cancellationToken);

    public Task<int> CountWarehouseStockWithStockAsync(
        WarehouseStockQueryFilter filter,
        CancellationToken cancellationToken = default) =>
        FilteredBalances(filter, applySearch: true, applyHideZero: false)
            .CountAsync(balance => balance.StockOnHand > 0, cancellationToken);

    public async Task<IReadOnlyList<WarehouseStockProjectionRow>> GetWarehouseStockPageAsync(
        WarehouseStockQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        IOrderedQueryable<StockBalance> ordered = ApplySort(
            FilteredBalances(filter, applySearch: true, applyHideZero: filter.HideZero),
            filter);

        return await ordered
            .ThenBy(balance => balance.ProductId)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .Select(balance => new WarehouseStockProjectionRow(
                balance.ProductId,
                balance.Product.Name.Value,
                balance.Product.Code,
                balance.Product.Category.Id,
                balance.Product.Category.Name.Value,
                balance.StockOnHand,
                balance.ReservedOnHand,
                balance.DamagedQty,
                _dbContext.InventoryMovements
                    .Where(movement => movement.WarehouseId == balance.WarehouseId &&
                        movement.ProductId == balance.ProductId)
                    .Max(movement => (DateTime?)movement.EffectiveAtUtc)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockCategoryOption>> GetWarehouseStockCategoriesAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.StockBalances
            .AsNoTracking()
            .Where(balance => balance.WarehouseId == warehouseId)
            .Select(balance => new
            {
                CategoryId = balance.Product.Category.Id,
                CategoryName = balance.Product.Category.Name.Value
            })
            .GroupBy(item => new { item.CategoryId, item.CategoryName })
            .Select(group => new StockCategoryOption(
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Count()))
            .ToListAsync(cancellationToken);

        return categories
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToList();
    }

    private IQueryable<StockBalance> FilteredBalances(
        WarehouseStockQueryFilter filter,
        bool applySearch,
        bool applyHideZero)
    {
        IQueryable<StockBalance> query = _dbContext.StockBalances
            .AsNoTracking()
            .Where(balance => balance.WarehouseId == filter.WarehouseId);

        if (applySearch && filter.SearchTerm is not null)
        {
            string term = filter.SearchTerm.ToLowerInvariant();
            query = query.Where(balance =>
                balance.Product.Name.Value.ToLower().Contains(term) ||
                balance.Product.Code.ToLower().Contains(term));
        }

        if (filter.CategoryId is int categoryId)
            query = query.Where(balance => balance.Product.Category.Id == categoryId);

        query = filter.Status switch
        {
            StockStatusFilter.Available => query.Where(balance =>
                balance.StockOnHand - balance.ReservedOnHand > 0 && balance.ReservedOnHand == 0),
            StockStatusFilter.PartiallyReserved => query.Where(balance =>
                balance.StockOnHand - balance.ReservedOnHand > 0 && balance.ReservedOnHand > 0),
            StockStatusFilter.FullyReserved => query.Where(balance =>
                balance.StockOnHand > 0 && balance.StockOnHand - balance.ReservedOnHand == 0),
            StockStatusFilter.OutOfStock => query.Where(balance => balance.StockOnHand == 0),
            _ => query
        };

        if (applyHideZero)
        {
            query = query.Where(balance =>
                balance.StockOnHand != 0 || balance.ReservedOnHand != 0 || balance.DamagedQty != 0);
        }

        return query;
    }

    private static IOrderedQueryable<StockBalance> ApplySort(
        IQueryable<StockBalance> query,
        WarehouseStockQueryFilter filter)
    {
        if (filter.Sort == StockSortColumn.ProductName)
        {
            return filter.Descending
                ? query.OrderByDescending(balance => balance.Product.Name.Value)
                : query.OrderBy(balance => balance.Product.Name.Value);
        }

        if (filter.Sort == StockSortColumn.Physical)
        {
            return filter.Descending
                ? query.OrderByDescending(balance => balance.StockOnHand)
                : query.OrderBy(balance => balance.StockOnHand);
        }

        if (filter.Sort == StockSortColumn.Reserved)
        {
            return filter.Descending
                ? query.OrderByDescending(balance => balance.ReservedOnHand)
                : query.OrderBy(balance => balance.ReservedOnHand);
        }

        if (filter.Sort == StockSortColumn.Damaged)
        {
            return filter.Descending
                ? query.OrderByDescending(balance => balance.DamagedQty)
                : query.OrderBy(balance => balance.DamagedQty);
        }

        return filter.Descending
            ? query.OrderByDescending(balance => balance.StockOnHand - balance.ReservedOnHand)
            : query.OrderBy(balance => balance.StockOnHand - balance.ReservedOnHand);
    }

    private IQueryable<InventoryMovement> LedgerQuery(int warehouseId, int? productId)
    {
        var query = _dbContext.InventoryMovements
            .AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId);

        if (productId.HasValue)
            query = query.Where(x => x.ProductId == productId.Value);

        return query;
    }
}
