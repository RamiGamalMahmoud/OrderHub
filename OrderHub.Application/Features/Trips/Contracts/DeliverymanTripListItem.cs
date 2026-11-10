using System;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.Contracts;

public record DeliverymanTripListItem(
    int Id,
    long TripNumber,
    int SequenceYear,
    int SequenceMonth,
    string Title,
    TripPartyType FromType,
    string FromName,
    TripPartyType ToType,
    string ToName,
    WorkRecordStatus Status,
    TripDisplayState DisplayState,
    int AttemptCount,
    int? CurrentAttemptNumber,
    bool HasFailedAttempt,
    DateTime? ScheduledAtUtc,
    DateTime CreatedAt,
    int Version,
    bool IsEditable);
