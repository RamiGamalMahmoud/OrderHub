using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Trips.GetTripDetails;

internal sealed class GetTripDetailsQueryHandler(ITripReader reader)
    : IRequestHandler<GetTripDetailsQuery, Result<TripDetails>>
{
    public async Task<Result<TripDetails>> Handle(
        GetTripDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var details = await reader.GetDetailsAsync(
            request.Id,
            request.HistoryPage,
            request.HistoryPageSize,
            cancellationToken);

        return details is null
            ? Result<TripDetails>.Failure("المشوار غير موجود.", "NotFound")
            : Result<TripDetails>.Success(details);
    }
}
