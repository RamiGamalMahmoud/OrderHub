using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Update;

public static class UpdateOrder
{
    public record Command(int OrderId, OrderInput.Order Order) : IRequest<Result>;

    internal class Handler : IRequestHandler<Command, Result>
    {
        private readonly IOrderStore _orderStore;

        public Handler(IOrderStore orderStore)
        {
            _orderStore = orderStore;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            return await _orderStore.UpdateOrder(request.OrderId, request.Order, cancellationToken);
        }
    }
}
