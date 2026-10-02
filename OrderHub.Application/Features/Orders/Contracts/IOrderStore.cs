using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Contracts;

public interface IOrderStore
{
    Task<Result<int>> Create(OrderInput.Order order, CancellationToken cancellationToken = default);
    Task<Result> UpdateOrder(int orderId, OrderInput.Order order, CancellationToken cancellationToken = default);
    Task<Result> Delete(int orderId);
}
