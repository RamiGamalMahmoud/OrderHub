#nullable enable

using System.Collections.Generic;
using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.GetTripLookups;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read)]
public record GetTripLookupsQuery(
    TripLookupKind Kind,
    string? SearchTerm = null,
    int Take = 20) : IRequest<IReadOnlyList<TripLookupItem>>;
