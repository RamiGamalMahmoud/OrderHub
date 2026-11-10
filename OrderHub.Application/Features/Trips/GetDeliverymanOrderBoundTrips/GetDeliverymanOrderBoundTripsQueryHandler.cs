using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Trips.Contracts;

namespace OrderHub.Application.Features.Trips.GetDeliverymanOrderBoundTrips;

internal sealed class GetDeliverymanOrderBoundTripsQueryHandler(ITripReader reader)
    : IRequestHandler<GetDeliverymanOrderBoundTripsQuery, PagedResult<OrderBoundTripListItem>>
{
    public Task<PagedResult<OrderBoundTripListItem>> Handle(
        GetDeliverymanOrderBoundTripsQuery request,
        CancellationToken cancellationToken) =>
        reader.GetDeliverymanOrderBoundTripsPageAsync(
            request.DeliverymanId,
            request.Page,
            request.PageSize,
            cancellationToken);
}
