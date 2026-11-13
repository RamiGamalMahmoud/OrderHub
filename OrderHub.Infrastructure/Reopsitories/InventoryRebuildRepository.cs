using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class InventoryRebuildRepository : IInventoryRebuildRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryRebuildRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<StockBalance>> LockBalancesForRebuildAsync(
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true
            && _dbContext.Database.CurrentTransaction is not null)
        {
            if (warehouseId is int scopedId)
            {
                await _dbContext.StockBalances
                    .FromSqlInterpolated(
                        $"SELECT * FROM stock_balances WHERE warehouse_id = {scopedId} ORDER BY product_id FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }
            else
            {
                await _dbContext.StockBalances
                    .FromSqlRaw("SELECT * FROM stock_balances ORDER BY warehouse_id, product_id FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }
        }

        IQueryable<StockBalance> query = _dbContext.StockBalances;
        if (warehouseId is int filterId)
            query = query.Where(balance => balance.WarehouseId == filterId);

        return await query
            .OrderBy(balance => balance.WarehouseId)
            .ThenBy(balance => balance.ProductId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LedgerBalanceRow>> GetLedgerBalancesAsync(
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<InventoryMovement> scoped = _dbContext.InventoryMovements.AsNoTracking();
        if (warehouseId is int scopedId)
            scoped = scoped.Where(movement => movement.WarehouseId == scopedId);

        var plainMovements = await scoped
            .Where(movement => movement.MovementType != InventoryMovementType.Reversal)
            .GroupBy(movement => new
            {
                movement.WarehouseId,
                movement.ProductId,
                movement.MovementType,
                movement.Direction
            })
            .Select(group => new
            {
                group.Key.WarehouseId,
                group.Key.ProductId,
                group.Key.MovementType,
                group.Key.Direction,
                Total = group.Sum(movement => movement.Quantity)
            })
            .ToListAsync(cancellationToken);

        // A reversal is classified by the type of the movement it reverses, so it nets out
        // exactly where the original was counted.
        var reversalMovements = await (
            from reversal in scoped.Where(movement =>
                movement.MovementType == InventoryMovementType.Reversal &&
                movement.ReversalOfMovementId != null)
            join original in _dbContext.InventoryMovements.AsNoTracking()
                on reversal.ReversalOfMovementId equals original.Id
            group new { reversal, original } by new
            {
                reversal.WarehouseId,
                reversal.ProductId,
                original.MovementType,
                reversal.Direction
            }
            into grouped
            select new
            {
                grouped.Key.WarehouseId,
                grouped.Key.ProductId,
                grouped.Key.MovementType,
                grouped.Key.Direction,
                Total = grouped.Sum(row => row.reversal.Quantity)
            }).ToListAsync(cancellationToken);

        var totals = new Dictionary<(int WarehouseId, int ProductId), (int Sound, int Damaged)>();

        void Accumulate(
            int movementWarehouseId,
            int productId,
            InventoryMovementType movementType,
            InventoryDirection direction,
            int quantity)
        {
            int signed = direction == InventoryDirection.In ? quantity : -quantity;
            totals.TryGetValue((movementWarehouseId, productId), out var pair);
            totals[(movementWarehouseId, productId)] = IsDamagedType(movementType)
                ? (pair.Sound, pair.Damaged + signed)
                : (pair.Sound + signed, pair.Damaged);
        }

        foreach (var row in plainMovements)
            Accumulate(row.WarehouseId, row.ProductId, row.MovementType, row.Direction, row.Total);

        foreach (var row in reversalMovements)
            Accumulate(row.WarehouseId, row.ProductId, row.MovementType, row.Direction, row.Total);

        return totals
            .Select(pair => new LedgerBalanceRow(
                pair.Key.WarehouseId,
                pair.Key.ProductId,
                pair.Value.Sound,
                pair.Value.Damaged))
            .ToList();
    }

    public async Task<IReadOnlyList<ReservedBalanceRow>> GetReservedBalancesAsync(
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrderInventoryAllocation> scoped = _dbContext.OrderInventoryAllocations
            .AsNoTracking()
            .Where(allocation =>
                allocation.Status == OrderAllocationStatus.Active &&
                allocation.WarehouseId != null);

        if (warehouseId is int scopedId)
            scoped = scoped.Where(allocation => allocation.WarehouseId == scopedId);

        var rows = await scoped
            .GroupBy(allocation => new { allocation.WarehouseId, allocation.ProductId })
            .Select(group => new
            {
                group.Key.WarehouseId,
                group.Key.ProductId,
                Total = group.Sum(allocation => allocation.Qty - allocation.IssuedQty)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ReservedBalanceRow(
                row.WarehouseId!.Value,
                row.ProductId,
                row.Total))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<int, string>> GetProductNamesAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
            return new Dictionary<int, string>();

        int[] ids = productIds.Distinct().ToArray();
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product => ids.Contains(product.Id))
            .Select(product => new { product.Id, Name = product.Name.Value })
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken);
    }

    public void AddBalance(StockBalance balance) => _dbContext.StockBalances.Add(balance);

    public void AddLog(InventoryRebuildLog log) => _dbContext.InventoryRebuildLogs.Add(log);

    public async Task<IReadOnlyList<RebuildRunRow>> GetHistoryAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        int limit = Math.Clamp(take, 1, 50);

        return await (
            from log in _dbContext.InventoryRebuildLogs.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking()
                on log.PerformedByUserId equals user.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on log.WarehouseId equals warehouse.Id into warehouses
            from warehouse in warehouses.DefaultIfEmpty()
            orderby log.CreatedAt descending, log.Id descending
            select new RebuildRunRow(
                log.Id,
                log.CreatedAt,
                user.DisplayName ?? user.UserName,
                log.Scope,
                warehouse == null ? null : warehouse.Name.Value,
                log.InspectedPairs,
                log.ChangedPairs))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<RebuildLogDetails> GetDetailsAsync(
        int logId,
        CancellationToken cancellationToken = default)
    {
        var header = await (
            from log in _dbContext.InventoryRebuildLogs.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking()
                on log.PerformedByUserId equals user.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on log.WarehouseId equals warehouse.Id into warehouses
            from warehouse in warehouses.DefaultIfEmpty()
            where log.Id == logId
            select new RebuildLogDetails(
                log.Id,
                log.CreatedAt,
                user.DisplayName ?? user.UserName,
                log.Scope,
                warehouse == null ? null : warehouse.Name.Value,
                log.InspectedPairs,
                log.ChangedPairs,
                Array.Empty<RebuildLogLineRow>()))
            .SingleOrDefaultAsync(cancellationToken);

        if (header is null)
            return null;

        var lines = await (
            from line in _dbContext.InventoryRebuildLogLines.AsNoTracking()
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on line.WarehouseId equals warehouse.Id
            join product in _dbContext.Products.AsNoTracking()
                on line.ProductId equals product.Id
            where line.RebuildLogId == logId
            orderby warehouse.Name.Value, product.Name.Value, line.ProductId
            select new RebuildLogLineRow(
                line.WarehouseId,
                warehouse.Name.Value,
                line.ProductId,
                product.Name.Value,
                product.Code,
                line.BeforeStockOnHand,
                line.BeforeReservedOnHand,
                line.BeforeDamagedQty,
                line.AfterStockOnHand,
                line.AfterReservedOnHand,
                line.AfterDamagedQty))
            .ToListAsync(cancellationToken);

        return header with { Lines = lines };
    }

    private static bool IsDamagedType(InventoryMovementType movementType) =>
        movementType is InventoryMovementType.ReturnDamagedIn
            or InventoryMovementType.DamagedDisposal
            or InventoryMovementType.DamagedAdjustment;
}
