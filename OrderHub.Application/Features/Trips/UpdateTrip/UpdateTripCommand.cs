#nullable enable

using System;
using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.UpdateTrip;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record UpdateTripCommand(
    int Id,
    int ExpectedVersion,
    int DeliverymanId,
    string? Title,
    string? Description,
    TripPartyInput From,
    TripPartyInput To,
    DateTimeOffset? ScheduledAtUtc,
    string? EditNote,
    string? ReassignReason) : IRequest<Result<int>>;
