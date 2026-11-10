#nullable enable

using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.Contracts;

public record TripPartyInput(
    TripPartyType PartyType,
    int? PartyId,
    string? Name,
    string? Phone,
    string? Address,
    string? Location);
