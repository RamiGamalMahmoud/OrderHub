using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Stores;

public interface IOrderRepository
{
    void Add(Order order);
    Task<Order> GetById(int orderId);

    Task<int> GetLastEntitySequenceNumberAsync(
        RecipientType recipientType,
        int entityId,
        DateTime referenceDate,
        CancellationToken cancellationToken);
    Task<int> GetNextOrderNumberAsync(CancellationToken cancellationToken = default);
}
