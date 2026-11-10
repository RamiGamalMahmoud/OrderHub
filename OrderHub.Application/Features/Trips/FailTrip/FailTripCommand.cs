#nullable enable

using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.FailTrip;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record FailTripCommand(int Id, int ExpectedVersion, string? Reason) : IRequest<Result<int>>;
