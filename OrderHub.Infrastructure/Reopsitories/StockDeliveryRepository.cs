using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Deliveries.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class StockDeliveryRepository : IStockDeliveryRepository
{
    private readonly AppDbContext _dbContext;

    public StockDeliveryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(StockDeliveryRecord record) => _dbContext.StockDeliveryRecords.Add(record);

    public async Task<int> GetDeliveredQuantityAsync(
        int allocationId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.StockDeliveryRecords
            .AsNoTracking()
            .Where(record => record.AllocationId == allocationId)
            .SumAsync(record => (int?)record.Quantity, cancellationToken) ?? 0;

    public async Task<OrderDeliveryOverview> GetOrderDeliveryOverviewAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var allocations = await _dbContext.OrderInventoryAllocations
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
                allocation.IssuedQty
            })
            .ToListAsync(cancellationToken);

        var delivered = await _dbContext.StockDeliveryRecords
            .AsNoTracking()
            .Where(record => record.OrderId == orderId)
            .GroupBy(record => record.AllocationId)
            .Select(group => new { AllocationId = group.Key, Quantity = group.Sum(record => record.Quantity) })
            .ToDictionaryAsync(item => item.AllocationId, item => item.Quantity, cancellationToken);

        var rows = allocations
            .Select(allocation => new OrderDeliveryRow(
                allocation.Id,
                allocation.OrderItemId,
                allocation.ProductId,
                allocation.ProductName,
                allocation.WarehouseId,
                allocation.WarehouseName,
                allocation.IssuedQty,
                delivered.TryGetValue(allocation.Id, out int quantity) ? quantity : 0))
            .OrderBy(row => row.ProductName, StringComparer.Ordinal)
            .ThenBy(row => row.AllocationId)
            .ToList();

        var trips = await _dbContext.DeliveryTrips
            .AsNoTracking()
            .Where(trip => trip.OrderId == orderId)
            .OrderBy(trip => trip.WorkRecordNumber)
            .Select(trip => new DeliveryLinkOption(trip.Id, trip.WorkRecordNumber, trip.SequenceYear, trip.SequenceMonth))
            .ToListAsync(cancellationToken);

        var shipments = await _dbContext.Shipments
            .AsNoTracking()
            .Where(shipment => shipment.OrderId == orderId)
            .OrderBy(shipment => shipment.WorkRecordNumber)
            .Select(shipment => new DeliveryLinkOption(shipment.Id, shipment.WorkRecordNumber, shipment.SequenceYear, shipment.SequenceMonth))
            .ToListAsync(cancellationToken);

        return new OrderDeliveryOverview(rows, trips, shipments);
    }

    public async Task<bool> IsLinkValidAsync(
        int orderId,
        int? tripId,
        int? shipmentId,
        CancellationToken cancellationToken = default)
    {
        if (tripId.HasValue)
        {
            return await _dbContext.DeliveryTrips
                .AsNoTracking()
                .AnyAsync(trip => trip.Id == tripId.Value && trip.OrderId == orderId, cancellationToken);
        }

        if (shipmentId.HasValue)
        {
            return await _dbContext.Shipments
                .AsNoTracking()
                .AnyAsync(shipment => shipment.Id == shipmentId.Value && shipment.OrderId == orderId, cancellationToken);
        }

        return true;
    }
}
