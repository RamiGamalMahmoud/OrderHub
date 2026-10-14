using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.GetSummaries;

internal sealed class GetOrderSummariesQueryHandler(IOrderReader reader)
    : IRequestHandler<GetOrderSummariesQuery, OrderSummaryData>
{
    public Task<OrderSummaryData> Handle(GetOrderSummariesQuery request, CancellationToken cancellationToken) =>
        reader.GetSummariesAsync(request.OrderId, cancellationToken);
}
