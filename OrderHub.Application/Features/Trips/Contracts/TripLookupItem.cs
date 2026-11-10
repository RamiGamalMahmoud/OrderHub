#nullable enable

namespace OrderHub.Application.Features.Trips.Contracts;

public record TripLookupItem(
    int Id,
    string Name,
    string? Detail);
