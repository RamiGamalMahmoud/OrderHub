using MediatR;
using OrderHub.Application.Common.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Orders.Contracts;

namespace OrderHub.Application.Features.Orders.GetDetails;

internal sealed class GetOrderDetailsQueryHandler(IOrderReader reader) : IRequestHandler<GetOrderDetailsQuery, OrderDetails>
{
    public Task<OrderDetails> Handle(GetOrderDetailsQuery request, CancellationToken cancellationToken) =>
        reader.GetDetailsAsync(request.Id, cancellationToken);
}
