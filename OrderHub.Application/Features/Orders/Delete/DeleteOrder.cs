using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Delete;

public static class DeleteOrder
{
    public record Command(int OrderId) : IRequest<Result>;
    internal class Handler(IOrderStore orderStore) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            return await orderStore.Delete(request.OrderId);
        }
    }
}
