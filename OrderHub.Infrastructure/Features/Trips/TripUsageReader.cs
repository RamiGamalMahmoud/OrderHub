using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;

namespace OrderHub.Infrastructure.Features.Trips;

internal sealed class TripUsageReader(AppDbContext context) : ITripUsageReader
{
    public async Task<bool> HasDeliverymanTripsAsync(
        int deliverymanId,
        CancellationToken cancellationToken) =>
        await context.StandaloneTrips
            .AsNoTracking()
            .AnyAsync(trip => trip.DeliverymanId == deliverymanId, cancellationToken)
        || await context.StandaloneTripHistoryEntries
            .AsNoTracking()
            .AnyAsync(
                entry => entry.PreviousDeliverymanId == deliverymanId
                    || entry.NewDeliverymanId == deliverymanId,
                cancellationToken);

    public Task<bool> HasPartyTripsAsync(
        TripPartyType partyType,
        int partyId,
        CancellationToken cancellationToken) =>
        context.StandaloneTrips
            .AsNoTracking()
            .AnyAsync(
                trip => (trip.From.PartyType == partyType && trip.From.PartyId == partyId)
                    || (trip.To.PartyType == partyType && trip.To.PartyId == partyId),
                cancellationToken);
}
