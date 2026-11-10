namespace OrderHub.Application.Features.Trips.Contracts;

/// <summary>
/// Global trips counters across standalone and order-bound trips, grouped by
/// <see cref="OrderHub.Domain.Enums.WorkRecordStatus"/> (Pending = جديد + قيد التنفيذ).
/// </summary>
public record TripsStatistics(
    int TotalCount,
    int ActiveCount,
    int CompletedCount,
    int FailedCount,
    int CancelledCount);
