#nullable enable

using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.CancelTrip;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record CancelTripCommand(int Id, int ExpectedVersion, string? Reason) : IRequest<Result<int>>;
