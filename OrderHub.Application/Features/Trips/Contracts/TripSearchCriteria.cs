#nullable enable

using System;
using System.Collections.Generic;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.Contracts;

public record TripSearchCriteria(
    string? Search = null,
    IReadOnlyCollection<TripDisplayState>? DisplayStates = null,
    int? DeliverymanId = null,
    TripPartyType? PartyType = null,
    DateOnly? ScheduledFrom = null,
    DateOnly? ScheduledTo = null,
    DateOnly? CreatedFrom = null,
    DateOnly? CreatedTo = null);
