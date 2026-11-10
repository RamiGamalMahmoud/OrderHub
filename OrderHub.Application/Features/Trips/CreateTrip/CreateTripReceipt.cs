using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.CreateTrip;

public record CreateTripReceipt(
    int Id,
    int Version,
    WorkRecordStatus Status,
    long TripNumber,
    int SequenceYear,
    int SequenceMonth);
