using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.CompleteTrip;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record CompleteTripCommand(int Id, int ExpectedVersion) : IRequest<Result<int>>;
