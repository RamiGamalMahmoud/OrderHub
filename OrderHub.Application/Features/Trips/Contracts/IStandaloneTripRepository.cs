#nullable enable

using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Common;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Application.Features.Trips.Contracts;

public interface IStandaloneTripRepository
{
    void Add(StandaloneTrip trip);

    Task<StandaloneTrip?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<StandaloneTrip?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<StandaloneTrip?> GetForEditAsync(int id, CancellationToken cancellationToken);

    Task<PagedResult<OrderBoundTripListItem>> GetOrderBoundTripsPageAsync(
        int deliverymanId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountOrderBoundTripsAsync(int deliverymanId, CancellationToken cancellationToken);
}
