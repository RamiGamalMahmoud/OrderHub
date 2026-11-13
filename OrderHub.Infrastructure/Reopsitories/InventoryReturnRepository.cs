using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class InventoryReturnRepository : IInventoryReturnRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryReturnRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(InventoryReturn record) => _dbContext.InventoryReturns.Add(record);

    public async Task<IReadOnlyList<OrderInventoryAllocation>> GetAllocationsForUpdateAsync(
        IReadOnlyCollection<int> allocationIds,
        CancellationToken cancellationToken = default)
    {
        int[] ids = allocationIds.Distinct().ToArray();
        return await _dbContext.OrderInventoryAllocations
            .Include(allocation => allocation.Order)
            .Include(allocation => allocation.OrderItem)
            .Include(allocation => allocation.Warehouse)
            .Where(allocation => ids.Contains(allocation.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetReturnedQuantityAsync(
        int allocationId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.InventoryReturnLines
            .AsNoTracking()
            .Where(line => line.AllocationId == allocationId)
            .SumAsync(line => (int?)(line.IntactQty + line.DamagedQty), cancellationToken) ?? 0;

    public async Task<OrderReturnContext> GetOrderReturnContextAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        string orderNumber = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => order.OrderNumber)
            .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;

        var rows = await _dbContext.OrderInventoryAllocations
            .AsNoTracking()
            .Where(allocation => allocation.OrderId == orderId
                && allocation.SourceKind == AllocationSourceKind.Warehouse
                && allocation.IssuedQty > 0)
            .Select(allocation => new
            {
                allocation.Id,
                allocation.OrderItemId,
                allocation.ProductId,
                ProductName = allocation.OrderItem.ProductName,
                WarehouseId = allocation.WarehouseId ?? 0,
                WarehouseName = allocation.Warehouse.Name.Value,
                allocation.IssuedQty,
                allocation.PendingReturnQty
            })
            .ToListAsync(cancellationToken);

        var returned = await _dbContext.InventoryReturnLines
            .AsNoTracking()
            .Where(line => line.Return.OrderId == orderId)
            .GroupBy(line => line.AllocationId)
            .Select(group => new
            {
                AllocationId = group.Key,
                Quantity = group.Sum(line => line.IntactQty + line.DamagedQty)
            })
            .ToDictionaryAsync(item => item.AllocationId, item => item.Quantity, cancellationToken);

        List<ReturnableAllocationRow> mapped = rows
            .Select(row => new ReturnableAllocationRow(
                row.Id,
                row.OrderItemId,
                row.ProductId,
                row.ProductName,
                row.WarehouseId,
                row.WarehouseName,
                row.IssuedQty,
                returned.TryGetValue(row.Id, out int quantity) ? quantity : 0,
                row.PendingReturnQty))
            .OrderBy(row => row.ProductName, System.StringComparer.Ordinal)
            .ThenBy(row => row.AllocationId)
            .ToList();

        return new OrderReturnContext(
            orderId,
            orderNumber,
            mapped.FirstOrDefault()?.WarehouseName ?? string.Empty,
            mapped);
    }

    public async Task<IReadOnlyList<PendingReturnRow>> GetPendingAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.OrderInventoryAllocations
            .AsNoTracking()
            .Where(allocation => allocation.WarehouseId == warehouseId
                && allocation.PendingReturnQty > 0)
            .Select(allocation => new
            {
                allocation.Id,
                allocation.OrderId,
                allocation.Order.OrderNumber,
                allocation.ProductId,
                ProductName = allocation.OrderItem.ProductName,
                WarehouseId = allocation.WarehouseId ?? 0,
                WarehouseName = allocation.Warehouse.Name.Value,
                allocation.IssuedQty,
                allocation.PendingReturnQty
            })
            .ToListAsync(cancellationToken);

        var returned = await _dbContext.InventoryReturnLines
            .AsNoTracking()
            .Where(line => line.Return.WarehouseId == warehouseId)
            .GroupBy(line => line.AllocationId)
            .Select(group => new
            {
                AllocationId = group.Key,
                Quantity = group.Sum(line => line.IntactQty + line.DamagedQty)
            })
            .ToDictionaryAsync(item => item.AllocationId, item => item.Quantity, cancellationToken);

        return rows
            .OrderBy(row => row.OrderNumber, System.StringComparer.Ordinal)
            .ThenBy(row => row.ProductName, System.StringComparer.Ordinal)
            .Select(row => new PendingReturnRow(
                row.Id,
                row.OrderId,
                row.OrderNumber,
                row.ProductId,
                row.ProductName,
                row.WarehouseId,
                row.WarehouseName,
                row.IssuedQty,
                returned.TryGetValue(row.Id, out int quantity) ? quantity : 0,
                row.PendingReturnQty))
            .ToList();
    }

    public async Task<IReadOnlyList<ReturnableOrderRow>> GetReturnableOrdersAsync(
        int warehouseId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.OrderInventoryAllocations
            .AsNoTracking()
            .Where(allocation => allocation.WarehouseId == warehouseId
                && allocation.IssuedQty > 0)
            .Select(allocation => new
            {
                allocation.Id,
                allocation.OrderId,
                allocation.Order.OrderNumber,
                allocation.Order.CreatedAt,
                allocation.IssuedQty,
                allocation.PendingReturnQty
            })
            .ToListAsync(cancellationToken);

        var returned = await _dbContext.InventoryReturnLines
            .AsNoTracking()
            .Where(line => line.Return.WarehouseId == warehouseId)
            .GroupBy(line => line.AllocationId)
            .Select(group => new
            {
                AllocationId = group.Key,
                Quantity = group.Sum(line => line.IntactQty + line.DamagedQty)
            })
            .ToDictionaryAsync(item => item.AllocationId, item => item.Quantity, cancellationToken);

        return rows
            .Select(row => new
            {
                row.OrderId,
                row.OrderNumber,
                row.CreatedAt,
                Remaining = System.Math.Max(0, row.IssuedQty - (returned.TryGetValue(row.Id, out int quantity) ? quantity : 0)),
                Pending = row.PendingReturnQty
            })
            .Where(row => row.Remaining > 0 || row.Pending > 0)
            .GroupBy(row => new { row.OrderId, row.OrderNumber, row.CreatedAt })
            .Select(group => new
            {
                group.Key.OrderId,
                group.Key.OrderNumber,
                group.Key.CreatedAt,
                AllocationCount = group.Count(),
                RemainingQty = group.Sum(row => row.Remaining),
                PendingQty = group.Sum(row => row.Pending)
            })
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.OrderId)
            .Take(take)
            .Select(order => new ReturnableOrderRow(
                order.OrderId,
                order.OrderNumber,
                order.AllocationCount,
                order.RemainingQty,
                order.PendingQty))
            .ToList();
    }

    public async Task<IReadOnlyList<ReturnHistoryRow>> GetHistoryAsync(
        int warehouseId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.InventoryReturns
            .AsNoTracking()
            .Where(record => record.WarehouseId == warehouseId)
            .OrderByDescending(record => record.EffectiveAtUtc)
            .ThenByDescending(record => record.Id)
            .Take(take)
            .Select(record => new
            {
                record.Id,
                record.OrderId,
                record.Order.OrderNumber,
                record.EffectiveAtUtc,
                RecordedByName = record.RecordedBy.DisplayName ?? record.RecordedBy.UserName,
                WarehouseName = record.Warehouse.Name.Value,
                Lines = record.Lines
                    .OrderBy(line => line.Id)
                    .Select(line => new
                    {
                        line.ProductId,
                        ProductName = line.Product.Name.Value,
                        line.IntactQty,
                        line.DamagedQty,
                        line.PendingSettledQty
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(record => new ReturnHistoryRow(
                record.Id,
                record.OrderId,
                record.OrderNumber,
                record.EffectiveAtUtc,
                record.RecordedByName,
                record.WarehouseName,
                record.Lines
                    .Select(line => new ReturnHistoryLine(
                        line.ProductId,
                        line.ProductName,
                        line.IntactQty,
                        line.DamagedQty,
                        line.PendingSettledQty))
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<DamagedProductRow>> GetDamagedProductsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.StockBalances
            .AsNoTracking()
            .Where(balance => balance.WarehouseId == warehouseId && balance.DamagedQty > 0)
            .Select(balance => new
            {
                balance.WarehouseId,
                WarehouseName = _dbContext.Warehouses
                    .Where(warehouse => warehouse.Id == balance.WarehouseId)
                    .Select(warehouse => warehouse.Name.Value)
                    .FirstOrDefault(),
                balance.ProductId,
                ProductName = balance.Product.Name.Value,
                balance.Product.Code,
                balance.DamagedQty
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => row.ProductName, System.StringComparer.Ordinal)
            .Select(row => new DamagedProductRow(
                row.WarehouseId,
                row.WarehouseName ?? string.Empty,
                row.ProductId,
                row.ProductName,
                row.Code,
                row.DamagedQty))
            .ToList();
    }
}
