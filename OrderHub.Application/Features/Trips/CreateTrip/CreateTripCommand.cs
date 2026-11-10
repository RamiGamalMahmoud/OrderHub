#nullable enable

using System;
using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.CreateTrip;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record CreateTripCommand(
    int DeliverymanId,
    string? Title,
    string? Description,
    TripPartyInput From,
    TripPartyInput To,
    DateTimeOffset? ScheduledAtUtc) : IRequest<Result<CreateTripReceipt>>;
