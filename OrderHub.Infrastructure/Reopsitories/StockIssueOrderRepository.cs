using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class StockIssueOrderRepository : IStockIssueOrderRepository
{
    private readonly AppDbContext _dbContext;

    public StockIssueOrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(StockIssueOrder order) => _dbContext.StockIssueOrders.Add(order);

    public void RemoveBatch(StockIssueBatch batch) => _dbContext.StockIssueBatches.Remove(batch);

    public Task<StockIssueOrder> GetForBatchAsync(
        int batchId,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockIssueOrders
            .Include(order => order.Warehouse)
            .Include(order => order.Lines).ThenInclude(line => line.Batches)
            .Include(order => order.Lines).ThenInclude(line => line.Allocation)
            .Where(order => order.Lines.Any(line => line.Batches.Any(batch => batch.Id == batchId)))
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);

    public Task<StockIssueOrder> GetForUpdateAsync(
        int issueOrderId,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockIssueOrders
            .Include(order => order.Warehouse)
            .Include(order => order.Lines).ThenInclude(line => line.Batches)
            .Include(order => order.Lines).ThenInclude(line => line.Allocation)
            .AsSplitQuery()
            .SingleOrDefaultAsync(order => order.Id == issueOrderId, cancellationToken);

    public Task<StockIssueOrder> GetDetailsAsync(
        int issueOrderId,
        CancellationToken cancellationToken = default) =>
        DetailsQuery()
            .AsSplitQuery()
            .SingleOrDefaultAsync(order => order.Id == issueOrderId, cancellationToken);

    public async Task<IReadOnlyList<StockIssueOrder>> GetForOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default) =>
        await DetailsQuery()
            .Where(order => order.OrderId == orderId)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockIssueOrder>> GetActiveForOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.StockIssueOrders
            .Include(order => order.Warehouse)
            .Include(order => order.Lines).ThenInclude(line => line.Batches)
            .Where(order => order.OrderId == orderId &&
                (order.Status == WorkRecordStatus.Pending ||
                 order.Status == WorkRecordStatus.PartiallyExecuted ||
                 order.Status == WorkRecordStatus.CancelRequested))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockIssueOrder>> GetScopedAsync(
        int? warehouseId,
        int? requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StockIssueOrder> query = DetailsQuery();

        if (warehouseId.HasValue)
            query = query.Where(order => order.WarehouseId == warehouseId.Value);
        else if (requestedByUserId.HasValue)
            query = query.Where(order => order.RequestedByUserId == requestedByUserId.Value);

        return await query
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetActiveRequestedByAllocationAsync(
        IReadOnlyCollection<int> allocationIds,
        CancellationToken cancellationToken = default)
    {
        if (allocationIds.Count == 0)
            return new Dictionary<int, int>();

        var rows = await (
            from line in _dbContext.StockIssueLines.AsNoTracking()
            join order in _dbContext.StockIssueOrders.AsNoTracking()
                on line.IssueOrderId equals order.Id
            where allocationIds.Contains(line.AllocationId) &&
                (order.Status == WorkRecordStatus.Pending ||
                 order.Status == WorkRecordStatus.PartiallyExecuted ||
                 order.Status == WorkRecordStatus.CancelRequested)
            group line by line.AllocationId
            into grouped
            select new { AllocationId = grouped.Key, Qty = grouped.Sum(line => line.RequestedQty) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.AllocationId, row => row.Qty);
    }

    public async Task<IReadOnlyCollection<int>> GetCancelRequestedAllocationIdsAsync(
        IReadOnlyCollection<int> allocationIds,
        CancellationToken cancellationToken = default)
    {
        if (allocationIds.Count == 0)
            return [];

        return await (
            from line in _dbContext.StockIssueLines.AsNoTracking()
            join order in _dbContext.StockIssueOrders.AsNoTracking()
                on line.IssueOrderId equals order.Id
            where allocationIds.Contains(line.AllocationId) &&
                order.Status == WorkRecordStatus.CancelRequested
            select line.AllocationId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private IQueryable<StockIssueOrder> DetailsQuery() =>
        _dbContext.StockIssueOrders
            .AsNoTracking()
            .Include(order => order.Order)
            .Include(order => order.Warehouse)
            .Include(order => order.RequestedBy)
            .Include(order => order.AssignedTo)
            .Include(order => order.CancelRequestedBy)
            .Include(order => order.Lines).ThenInclude(line => line.Product)
            .Include(order => order.Lines).ThenInclude(line => line.Batches).ThenInclude(batch => batch.RecordedBy);
}
