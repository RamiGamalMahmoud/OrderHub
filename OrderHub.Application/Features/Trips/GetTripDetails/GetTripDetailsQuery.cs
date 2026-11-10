using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.GetTripDetails;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read)]
public record GetTripDetailsQuery(
    int Id,
    int HistoryPage = 1,
    int HistoryPageSize = 10) : IRequest<Result<TripDetails>>;
