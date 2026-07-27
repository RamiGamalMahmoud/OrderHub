using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Get;

public static class GetOrder
{
    public record Query(int Id) : IRequest<OrderDto>;
    public record OrderDto;
    internal class Handler : IRequestHandler<Query, OrderDto>
    {
        public Task<OrderDto> Handle(Query request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
