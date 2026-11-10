#nullable enable

using System;

namespace OrderHub.Application.Features.Trips.Contracts;

public record TripEditorData(
    int Id,
    int Version,
    bool IsEditable,
    int DeliverymanId,
    string DeliverymanName,
    string Title,
    string? Description,
    TripPartyInput From,
    TripPartyInput To,
    DateTime? ScheduledAtUtc);
