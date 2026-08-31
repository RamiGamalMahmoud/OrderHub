using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Stores;

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

    public async Task<int> GetLastEntitySequenceNumberAsync(
        RecipientType recipientType,
        int entityId,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        return await _dbContext.OrderEntitySequences
            .Where(sequence =>
                sequence.RecipientType == recipientType
                && sequence.EntityId == entityId
                && sequence.SequenceYear == referenceDate.Year
                && sequence.SequenceMonth == referenceDate.Month)
            .Select(sequence => (int?)sequence.SequenceNumber)
            .MaxAsync(cancellationToken)
            ?? 0;
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
