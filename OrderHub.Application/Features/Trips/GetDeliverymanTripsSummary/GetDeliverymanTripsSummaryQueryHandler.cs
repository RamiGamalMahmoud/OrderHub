using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Features.Trips.Contracts;

namespace OrderHub.Application.Features.Trips.GetDeliverymanTripsSummary;

internal sealed class GetDeliverymanTripsSummaryQueryHandler(ITripReader reader)
    : IRequestHandler<GetDeliverymanTripsSummaryQuery, DeliverymanTripsSummary>
{
    public Task<DeliverymanTripsSummary> Handle(
        GetDeliverymanTripsSummaryQuery request,
        CancellationToken cancellationToken) =>
        reader.GetDeliverymanSummaryAsync(request.DeliverymanId, cancellationToken);
}
