using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Trips.Contracts;

namespace OrderHub.Application.Features.Trips.GetTripsPage;

internal sealed class GetTripsPageQueryHandler(ITripReader reader)
    : IRequestHandler<GetTripsPageQuery, PagedResult<TripListItem>>
{
    public Task<PagedResult<TripListItem>> Handle(
        GetTripsPageQuery request,
        CancellationToken cancellationToken) =>
        reader.GetPageAsync(request.Criteria, request.Page, request.PageSize, cancellationToken);
}
