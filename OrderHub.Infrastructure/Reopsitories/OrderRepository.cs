using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces;
using System.Data.Common;
using OrderHub.Domain.Models;
using System;
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
            .Include(order => order.DeliverySteps)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task<Order> GetForLifecycleAsync(int orderId, CancellationToken cancellationToken = default) =>
        _dbContext.Orders.Include(order => order.DeliverySteps).SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

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

    public async Task<int> GetNextOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        string pattern = $"%-{DateTime.Now:yyyy-MM-dd}-%";

        string lastOrderNumber = await _dbContext
            .Orders
            .Where(o => EF.Functions.Like(o.OrderNumber, pattern))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastOrderNumber))
            return 1;

        string numberPart = lastOrderNumber.Split('-').LastOrDefault();

        int number = int.Parse(numberPart);

        return number + 1;
    }
}
