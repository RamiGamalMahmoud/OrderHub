#nullable enable

using System;
using System.Collections.Generic;
using OrderHub.Application.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.Contracts;

public record TripDetails(
    int Id,
    long TripNumber,
    int SequenceYear,
    int SequenceMonth,
    string Title,
    string? Description,
    int DeliverymanId,
    string DeliverymanName,
    TripPartyData From,
    TripPartyData To,
    DateTime? ScheduledAtUtc,
    DateTime CreatedAt,
    DateTime StatusChangedAtUtc,
    WorkRecordStatus Status,
    TripDisplayState DisplayState,
    int? CurrentAttemptNumber,
    int AttemptCount,
    bool HasFailedAttempt,
    int Version,
    bool IsEditable,
    IReadOnlyList<TripAttemptData> Attempts,
    PagedResult<TripHistoryEntryData> History);

public record TripPartyData(
    TripPartyType PartyType,
    int? PartyId,
    string Name,
    string? Phone,
    string? Address,
    string? Location);

public record TripAttemptData(
    int AttemptNumber,
    WorkRecordStatus Status,
    DateTime? StartedAtUtc,
    DateTime? EndedAtUtc,
    string? Reason,
    DateTime CreatedAtUtc);

public record TripHistoryEntryData(
    int Id,
    StandaloneTripEventType EventType,
    int? AttemptNumber,
    WorkRecordStatus? PreviousStatus,
    WorkRecordStatus? NewStatus,
    int? PreviousDeliverymanId,
    int? NewDeliverymanId,
    string? PreviousDeliverymanName,
    string? NewDeliverymanName,
    long? PreviousTripNumber,
    long? NewTripNumber,
    string? Reason,
    int ChangedByUserId,
    string ChangedByUserName,
    DateTime ChangedAtUtc,
    string Source);
