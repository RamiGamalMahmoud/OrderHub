#nullable enable

using System;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.Contracts;

public record TripListItem(
    int Id,
    TripSource Source,
    long TripNumber,
    int SequenceYear,
    int SequenceMonth,
    string? Title,
    int DeliverymanId,
    string DeliverymanName,
    TripPartyType? FromType,
    string? FromName,
    TripPartyType? ToType,
    string? ToName,
    DateTime? ScheduledAtUtc,
    WorkRecordStatus Status,
    TripDisplayState DisplayState,
    int? CurrentAttemptNumber,
    int AttemptCount,
    bool HasFailedAttempt,
    DateTime CreatedAt,
    int Version,
    bool IsEditable,
    int? OrderId,
    string? OrderNumber);
