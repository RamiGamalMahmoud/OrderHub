using System.Threading;
using System.Threading.Tasks;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Interfaces;

public interface ITripUsageReader
{
    Task<bool> HasDeliverymanTripsAsync(int deliverymanId, CancellationToken cancellationToken);

    Task<bool> HasPartyTripsAsync(
        TripPartyType partyType,
        int partyId,
        CancellationToken cancellationToken);
}
