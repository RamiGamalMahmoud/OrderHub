using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Update;

internal class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, Result>
{
    private readonly IOrderStore _orderStore;

    public UpdateOrderHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<Result> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        return await _orderStore.UpdateOrder(request.OrderId, request.Order, cancellationToken);
    }
}
