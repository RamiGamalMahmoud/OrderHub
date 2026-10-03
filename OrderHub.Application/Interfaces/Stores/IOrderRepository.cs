using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Stores;

public interface IOrderRepository
{
    void Add(Order order);
    Task<Order> GetById(int orderId);
    Task<int> GetNextOrderNumberAsync(CancellationToken cancellationToken = default);
}
