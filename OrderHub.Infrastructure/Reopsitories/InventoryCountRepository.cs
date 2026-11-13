using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class InventoryCountRepository : IInventoryCountRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryCountRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(InventoryCount count) => _dbContext.InventoryCounts.Add(count);

    public async Task<InventoryCount> GetForUpdateAsync(
        int countId,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true
            && _dbContext.Database.CurrentTransaction is not null)
        {
            await _dbContext.InventoryCounts
                .FromSqlInterpolated($"SELECT * FROM inventory_counts WHERE id = {countId} FOR UPDATE")
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        return await _dbContext.InventoryCounts
            .Include(count => count.Lines)
                .ThenInclude(line => line.Product)
            .Include(count => count.Warehouse)
            .SingleOrDefaultAsync(count => count.Id == countId, cancellationToken);
    }

    public Task<bool> HasActiveLineAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryCountLines.AsNoTracking().AnyAsync(
            line => line.WarehouseId == warehouseId &&
                line.ProductId == productId &&
                line.IsActive,
            cancellationToken);

    public async Task<BookQuantities> GetBookQuantitiesAsync(
        int warehouseId,
        int productId,
        DateTime upToUtc,
        CancellationToken cancellationToken = default)
    {
        DateTime cutoff = DateTime.SpecifyKind(upToUtc, DateTimeKind.Utc);
        var movements = await _dbContext.InventoryMovements
            .AsNoTracking()
            .Where(movement => movement.WarehouseId == warehouseId &&
                movement.ProductId == productId &&
                movement.EffectiveAtUtc <= cutoff)
            .Select(movement => new
            {
                movement.Id,
                movement.MovementType,
                movement.Direction,
                movement.Quantity,
                movement.ReversalOfMovementId
            })
            .ToListAsync(cancellationToken);

        if (movements.Count == 0)
            return new BookQuantities(0, 0);

        int[] reversalIds = movements
            .Where(movement => movement.MovementType == InventoryMovementType.Reversal &&
                movement.ReversalOfMovementId is not null)
            .Select(movement => movement.ReversalOfMovementId!.Value)
            .Distinct()
            .ToArray();

        Dictionary<int, InventoryMovementType> originalTypes = reversalIds.Length == 0
            ? []
            : await _dbContext.InventoryMovements
                .AsNoTracking()
                .Where(movement => reversalIds.Contains(movement.Id))
                .Select(movement => new { movement.Id, movement.MovementType })
                .ToDictionaryAsync(row => row.Id, row => row.MovementType, cancellationToken);

        int sound = 0;
        int damaged = 0;
        foreach (var movement in movements)
        {
            bool isDamaged = movement.MovementType == InventoryMovementType.Reversal
                ? movement.ReversalOfMovementId is int originalId &&
                    originalTypes.TryGetValue(originalId, out InventoryMovementType originalType) &&
                    IsDamagedType(originalType)
                : IsDamagedType(movement.MovementType);

            int signed = movement.Direction == InventoryDirection.In
                ? movement.Quantity
                : -movement.Quantity;

            if (isDamaged)
                damaged += signed;
            else
                sound += signed;
        }

        return new BookQuantities(sound, damaged);
    }

    public async Task<IReadOnlyList<CountableProductRow>> GetCountableProductsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.StockBalances
            .AsNoTracking()
            .Where(balance => balance.WarehouseId == warehouseId)
            .Select(balance => new
            {
                balance.ProductId,
                ProductName = balance.Product.Name.Value,
                balance.Product.Code
            })
            .ToListAsync(cancellationToken);

        int[] activeProductIds = await _dbContext.InventoryCountLines
            .AsNoTracking()
            .Where(line => line.WarehouseId == warehouseId && line.IsActive)
            .Select(line => line.ProductId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var active = activeProductIds.ToHashSet();
        return products
            .Select(product => new CountableProductRow(
                product.ProductId,
                product.ProductName,
                product.Code,
                active.Contains(product.ProductId)))
            .OrderBy(product => product.ProductName)
            .ThenBy(product => product.ProductId)
            .ToList();
    }

    public async Task<IReadOnlyList<CountRow>> GetRowsAsync(
        int? warehouseId,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<InventoryCount> query = _dbContext.InventoryCounts.AsNoTracking();
        if (warehouseId is int scopedWarehouseId)
            query = query.Where(count => count.WarehouseId == scopedWarehouseId);

        return await (
            from count in query
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on count.WarehouseId equals warehouse.Id
            join startedBy in _dbContext.Users.AsNoTracking()
                on count.StartedByUserId equals startedBy.Id
            orderby count.CreatedAt descending, count.Id descending
            select new CountRow(
                count.Id,
                count.WarehouseId,
                warehouse.Name.Value,
                count.Scope,
                count.Status,
                count.SnapshotAtUtc,
                count.Lines.Count(),
                count.Lines.Count(line => line.CountedQty != null && line.CountedDamagedQty != null),
                startedBy.DisplayName ?? startedBy.UserName,
                count.CompletedAtUtc,
                count.DecidedAtUtc,
                count.DecisionNote))
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<CountDetails> GetDetailsAsync(
        int countId,
        CancellationToken cancellationToken = default)
    {
        var header = await (
            from count in _dbContext.InventoryCounts.AsNoTracking()
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on count.WarehouseId equals warehouse.Id
            join startedBy in _dbContext.Users.AsNoTracking()
                on count.StartedByUserId equals startedBy.Id
            join decidedBy in _dbContext.Users.AsNoTracking()
                on count.DecidedByUserId equals decidedBy.Id into decidedGroup
            from decidedBy in decidedGroup.DefaultIfEmpty()
            where count.Id == countId
            select new
            {
                count.Id,
                count.WarehouseId,
                WarehouseName = warehouse.Name.Value,
                count.Scope,
                count.Status,
                count.Version,
                count.SnapshotAtUtc,
                StartedByName = startedBy.DisplayName ?? startedBy.UserName,
                count.CompletedAtUtc,
                DecidedByName = decidedBy == null ? null : decidedBy.DisplayName ?? decidedBy.UserName,
                count.DecidedAtUtc,
                count.DecisionNote
            }).SingleOrDefaultAsync(cancellationToken);

        if (header is null)
            return null;

        var lineRows = await (
            from line in _dbContext.InventoryCountLines.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on line.ProductId equals product.Id
            where line.CountId == countId
            orderby product.Name.Value, line.ProductId
            select new
            {
                line.Id,
                line.ProductId,
                ProductName = product.Name.Value,
                ProductCode = product.Code,
                line.BookQty,
                line.BookDamagedQty,
                line.CountedQty,
                line.CountedDamagedQty,
                line.SnapshotAtUtc,
                line.SnapshotRecordedAtUtc,
                line.ReviewedAtUtc,
                ReviewDecision = line.ReviewDecision,
                line.ReviewNote
            }).ToListAsync(cancellationToken);

        int[] productIds = lineRows.Select(line => line.ProductId).ToArray();
        IReadOnlyCollection<MovementStamp> movements = productIds.Length == 0
            ? Array.Empty<MovementStamp>()
            : await _dbContext.InventoryMovements
                .AsNoTracking()
                .Where(movement => movement.WarehouseId == header.WarehouseId &&
                    productIds.Contains(movement.ProductId))
                .Select(movement => new MovementStamp(
                    movement.ProductId,
                    movement.EffectiveAtUtc,
                    movement.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        var lines = lineRows
            .Select(line => new CountLineRow(
                line.Id,
                line.ProductId,
                line.ProductName,
                line.ProductCode,
                line.BookQty,
                line.BookDamagedQty,
                line.CountedQty,
                line.CountedDamagedQty,
                line.SnapshotAtUtc,
                line.ReviewedAtUtc,
                line.ReviewDecision,
                line.ReviewNote,
                HasLateMovement(line.ProductId, line.SnapshotAtUtc, line.SnapshotRecordedAtUtc, line.ReviewedAtUtc, movements)))
            .ToList();

        return new CountDetails(
            header.Id,
            header.WarehouseId,
            header.WarehouseName,
            header.Scope,
            header.Status,
            header.Version,
            header.SnapshotAtUtc,
            header.StartedByName,
            header.CompletedAtUtc,
            header.DecidedByName,
            header.DecidedAtUtc,
            header.DecisionNote,
            IsKeeper: false,
            AllCounted: lines.Count > 0 && lines.All(line => line.IsCounted),
            HasLateMovements: lines.Any(line => line.LateMovement),
            lines);
    }

    public async Task<IReadOnlyList<int>> GetLateMovementLineIdsAsync(
        InventoryCount count,
        CancellationToken cancellationToken = default)
    {
        List<InventoryCountLine> lines = count.Lines.Where(line => line.IsActive).ToList();
        if (lines.Count == 0)
            return [];

        int[] productIds = lines.Select(line => line.ProductId).Distinct().ToArray();
        List<MovementStamp> movements = await _dbContext.InventoryMovements
            .AsNoTracking()
            .Where(movement => movement.WarehouseId == count.WarehouseId &&
                productIds.Contains(movement.ProductId))
            .Select(movement => new MovementStamp(
                movement.ProductId,
                movement.EffectiveAtUtc,
                movement.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return lines
            .Where(line => HasLateMovement(
                line.ProductId,
                line.SnapshotAtUtc,
                line.SnapshotRecordedAtUtc,
                line.ReviewedAtUtc,
                movements))
            .Select(line => line.Id)
            .ToList();
    }

    private static bool HasLateMovement(
        int productId,
        DateTime snapshotAtUtc,
        DateTime snapshotRecordedAtUtc,
        DateTime? reviewedAtUtc,
        IEnumerable<MovementStamp> movements)
    {
        DateTime baseline = reviewedAtUtc ?? snapshotRecordedAtUtc;
        return movements.Any(movement =>
            movement.ProductId == productId &&
            movement.EffectiveAtUtc <= snapshotAtUtc &&
            movement.CreatedAtUtc > baseline);
    }

    private readonly record struct MovementStamp(
        int ProductId,
        DateTime EffectiveAtUtc,
        DateTime CreatedAtUtc);

    private static bool IsDamagedType(InventoryMovementType movementType) =>
        movementType is InventoryMovementType.ReturnDamagedIn
            or InventoryMovementType.DamagedDisposal
            or InventoryMovementType.DamagedAdjustment;
}
