using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Features.Trips.Contracts;

namespace OrderHub.Application.Features.Trips.GetTripsStatistics;

internal sealed class GetTripsStatisticsQueryHandler(ITripReader reader)
    : IRequestHandler<GetTripsStatisticsQuery, TripsStatistics>
{
    public Task<TripsStatistics> Handle(
        GetTripsStatisticsQuery request,
        CancellationToken cancellationToken) =>
        reader.GetStatisticsAsync(cancellationToken);
}
