using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Trips.Contracts;

namespace OrderHub.Application.Features.Trips.GetDeliverymanStandaloneTrips;

internal sealed class GetDeliverymanStandaloneTripsQueryHandler(ITripReader reader)
    : IRequestHandler<GetDeliverymanStandaloneTripsQuery, PagedResult<DeliverymanTripListItem>>
{
    public Task<PagedResult<DeliverymanTripListItem>> Handle(
        GetDeliverymanStandaloneTripsQuery request,
        CancellationToken cancellationToken) =>
        reader.GetDeliverymanStandaloneTripsPageAsync(
            request.DeliverymanId,
            request.Page,
            request.PageSize,
            cancellationToken);
}
