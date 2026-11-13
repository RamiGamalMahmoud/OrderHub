using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces;
using System.Data.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public OrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order> GetById(int orderId)
    {
        return await _dbContext.Orders
            .Include(o => o.Client)
                .ThenInclude(c => c.Phone)
            .Include(o => o.Client)
                .ThenInclude(c => c.Address)
                    .ThenInclude(address => address.City)
            .Include(o => o.OrderItems)
            .SingleOrDefaultAsync(o => o.Id == orderId);
    }

    public void Add(Order order)
    {
        _dbContext.Orders.Add(order);
    }

    public Task<Order> GetForUpdateAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Properties)
            .Include(order => order.Allocations)
            .Include(order => order.Supplies)
            .Include(order => order.DeliverySteps)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task<Order> GetForIssueOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
        _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Client)
            .Include(order => order.OrderItems)
            .Include(order => order.Allocations).ThenInclude(allocation => allocation.Warehouse)
            .AsSplitQuery()
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetOrdersWithOpenWarehouseAllocationsAsync(
        int take,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Client)
            .Include(order => order.Allocations)
            .Where(order => !order.IsArchived
                && order.OrderStatus != OrderStatus.Delivered
                && order.OrderStatus != OrderStatus.Cancelled
                && order.Allocations.Any(allocation =>
                    allocation.SourceKind == AllocationSourceKind.Warehouse &&
                    allocation.Status == OrderAllocationStatus.Active))
            .OrderByDescending(order => order.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);

    public Task<Order> GetForLifecycleAsync(int orderId, CancellationToken cancellationToken = default) =>
        _dbContext.Orders
            .Include(order => order.Deliveryman)
            .Include(order => order.ShippingCarrier)
            .Include(order => order.Shipments)
            .Include(order => order.DeliveryTrips)
            .Include(order => order.DeliverySteps).ThenInclude(step => step.Deliveryman)
            .Include(order => order.DeliverySteps).ThenInclude(step => step.ShippingCarrier)
            .Include(order => order.DeliverySteps).ThenInclude(step => step.Shipments)
            .Include(order => order.DeliverySteps).ThenInclude(step => step.DeliveryTrips)
            .Include(order => order.Allocations)
            .AsSplitQuery()
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public async Task<DeliveryParty> FindDeliveryPartyAsync(DeliveryMethod method, int partyId,
        CancellationToken cancellationToken = default)
    {
        var name = method switch
        {
            DeliveryMethod.DeliveryMan => await _dbContext.Deliverymen.Where(party => party.Id == partyId)
                .Select(party => party.Name.Value).SingleOrDefaultAsync(cancellationToken),
            DeliveryMethod.ShippingCompany => await _dbContext.ShippingCarriers.Where(party => party.Id == partyId)
                .Select(party => party.Name.Value).SingleOrDefaultAsync(cancellationToken),
            _ => null
        };
        return string.IsNullOrWhiteSpace(name) ? null : new DeliveryParty(partyId, name);
    }

    public async Task TombstoneDeliveryStepAsync(OrderDeliveryStep step,
        CancellationToken cancellationToken = default)
    {
        if (step.Id <= 0)
            throw new InvalidOperationException("Tombstoning requires a persisted delivery step.");
        if (_dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Delivery step reassignment requires a transaction.");
        var rank = -step.Id;
        try
        {
            await _dbContext.OrderDeliverySteps.Where(item => item.Id == step.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.StepOrder, rank), cancellationToken);
        }
        catch (DbException exception)
        {
            throw new PersistenceException(exception.Message, exception);
        }
        var property = _dbContext.Entry(step).Property(item => item.StepOrder);
        // The row is already tombstoned; keep both tracked values in sync so EF does not
        // rewrite the old positive rank and collide with the replacement at that rank.
        property.OriginalValue = rank;
        property.CurrentValue = rank;
    }

    public async Task PrepareDeliveryStepReorderingAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Vacate positive ranks inside the caller's transaction. The final save restores
        // retained rows, deletes removed rows and inserts new rows without unique-index collisions.
        if (_dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Delivery step reordering requires a transaction.");
        try
        {
            await _dbContext.OrderDeliverySteps.Where(step => step.OrderId == order.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(step => step.StepOrder, step => -step.Id), cancellationToken);
        }
        catch (DbException exception)
        {
            throw new PersistenceException(exception.Message, exception);
        }
        foreach (var step in order.DeliverySteps)
        {
            var rank = _dbContext.Entry(step).Property(value => value.StepOrder);
            // Match EF's snapshot to the staged row so swaps do not form a dependency cycle.
            rank.OriginalValue = -step.Id;
            rank.IsModified = true;
        }
    }

    public void RemoveAllocations(IEnumerable<OrderInventoryAllocation> allocations) =>
        _dbContext.OrderInventoryAllocations.RemoveRange(allocations);

    public Task<int?> FindCreationRequestAsync(Guid requestId, int userId, CancellationToken cancellationToken = default) =>
        _dbContext.Orders.Where(order => order.CreationRequestId == requestId && order.StatusHistory.Any(history =>
            history.Source == OrderHub.Domain.Enums.OrderHistorySource.Creation && history.ChangedByUserId == userId))
            .Select(order => (int?)order.Id).SingleOrDefaultAsync(cancellationToken);
}
