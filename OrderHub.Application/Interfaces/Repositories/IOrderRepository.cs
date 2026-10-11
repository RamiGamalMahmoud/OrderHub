using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    void Add(Order order);
    Task<Order> GetById(int orderId);
    Task<Order> GetForUpdateAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order> GetForLifecycleAsync(int orderId, CancellationToken cancellationToken = default);
    Task PrepareDeliveryStepReorderingAsync(Order order, CancellationToken cancellationToken = default);
    Task<int?> FindCreationRequestAsync(Guid requestId, int userId, CancellationToken cancellationToken = default);
}
