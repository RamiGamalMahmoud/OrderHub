using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Contracts;

public interface IOrderStore
{
    Task<Result<int>> Create(OrderDetails.Order order, CancellationToken cancellationToken = default);
    Task<Result> UpdateOrder(int orderId, OrderDetails.Order order, CancellationToken cancellationToken = default);
    Task<Result> Delete(int orderId);
}
