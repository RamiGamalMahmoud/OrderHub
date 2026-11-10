#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;
using OrderHub.Infrastructure.Extensions;

namespace OrderHub.Infrastructure.Features.Trips;

internal sealed class StandaloneTripRepository(AppDbContext context) : IStandaloneTripRepository
{
    public void Add(StandaloneTrip trip) => context.StandaloneTrips.Add(trip);

    public Task<StandaloneTrip?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.StandaloneTrips
            .AsNoTracking()
            .Include(trip => trip.Attempts)
            .FirstOrDefaultAsync(trip => trip.Id == id, cancellationToken);

    public Task<StandaloneTrip?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        context.StandaloneTrips
            .Include(trip => trip.Attempts)
            .Include(trip => trip.History)
            .FirstOrDefaultAsync(trip => trip.Id == id, cancellationToken);

    public Task<StandaloneTrip?> GetForEditAsync(int id, CancellationToken cancellationToken) =>
        context.StandaloneTrips
            .Include(trip => trip.Attempts)
            .Include(trip => trip.Deliveryman)
            .FirstOrDefaultAsync(trip => trip.Id == id, cancellationToken);

    public Task<PagedResult<OrderBoundTripListItem>> GetOrderBoundTripsPageAsync(
        int deliverymanId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var query = context.DeliveryTrips
            .AsNoTracking()
            .Where(trip => trip.NumberingPartyId == deliverymanId
                && (trip.OrderId != null || trip.DeliveryStep != null))
            .OrderByDescending(trip => trip.CreatedAt)
            .ThenByDescending(trip => trip.Id)
            .Select(trip => new OrderBoundTripListItem(
                trip.Id,
                trip.WorkRecordNumber,
                trip.SequenceYear,
                trip.SequenceMonth,
                trip.OrderId ?? (trip.DeliveryStep != null ? trip.DeliveryStep.OrderId : null),
                trip.Order != null
                    ? trip.Order.OrderNumber
                    : trip.DeliveryStep != null ? trip.DeliveryStep.Order.OrderNumber : null,
                trip.Order != null
                    ? trip.Order.OrderStatus
                    : trip.DeliveryStep != null ? trip.DeliveryStep.Order.OrderStatus : (OrderStatus?)null,
                trip.Status,
                trip.CreatedAt));

        return query.ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    public Task<int> CountOrderBoundTripsAsync(int deliverymanId, CancellationToken cancellationToken) =>
        context.DeliveryTrips
            .AsNoTracking()
            .Where(trip => trip.NumberingPartyId == deliverymanId)
            .CountAsync(trip => trip.OrderId != null || trip.DeliveryStepId != null, cancellationToken);
}
