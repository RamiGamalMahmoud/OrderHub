namespace OrderHub.Application.Features.Trips.Contracts;

public record DeliverymanTripsSummary(
    int OpenCount,
    int FailedCount,
    int CompletedCount,
    int CancelledCount,
    int TotalCount);
