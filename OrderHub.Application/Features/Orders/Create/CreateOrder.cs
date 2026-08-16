using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Create;

public static class CreateOrder
{
    public record Command(OrderDetails.Order Order) : IRequest<Result<int>>;

    internal class Handler : IRequestHandler<Command, Result<int>>
    {
        private readonly IOrderStore _orderStore;

        public Handler(IOrderStore orderStore)
        {
            _orderStore = orderStore;
        }

        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            return await _orderStore.Create(request.Order, cancellationToken);
        }
    }
}
