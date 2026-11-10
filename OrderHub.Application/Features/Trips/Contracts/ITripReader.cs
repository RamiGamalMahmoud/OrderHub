#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Common;

namespace OrderHub.Application.Features.Trips.Contracts;

public interface ITripReader
{
    Task<PagedResult<TripListItem>> GetPageAsync(
        TripSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<TripDetails?> GetDetailsAsync(
        int id,
        int historyPage,
        int historyPageSize,
        CancellationToken cancellationToken);

    Task<TripEditorData?> GetEditorAsync(int id, CancellationToken cancellationToken);

    Task<DeliverymanTripsSummary> GetDeliverymanSummaryAsync(
        int deliverymanId,
        CancellationToken cancellationToken);

    Task<TripsStatistics> GetStatisticsAsync(CancellationToken cancellationToken);

    Task<PagedResult<DeliverymanTripListItem>> GetDeliverymanStandaloneTripsPageAsync(
        int deliverymanId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<OrderBoundTripListItem>> GetDeliverymanOrderBoundTripsPageAsync(
        int deliverymanId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
