using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    void Add(Order order);
    Task<Order> GetById(int orderId);
    Task<Order> GetForUpdateAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order> GetForLifecycleAsync(int orderId, CancellationToken cancellationToken = default);
    Task PrepareDeliveryStepReorderingAsync(Order order, CancellationToken cancellationToken = default);
    Task<int> GetNextOrderNumberAsync(CancellationToken cancellationToken = default);
}
