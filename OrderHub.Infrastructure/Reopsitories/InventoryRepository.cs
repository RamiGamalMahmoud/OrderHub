using Microsoft.EntityFrameworkCore;
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
