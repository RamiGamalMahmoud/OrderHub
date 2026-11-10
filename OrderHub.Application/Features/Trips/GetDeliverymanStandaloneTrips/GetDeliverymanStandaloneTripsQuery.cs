using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.GetDeliverymanStandaloneTrips;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read)]
public record GetDeliverymanStandaloneTripsQuery(
    int DeliverymanId,
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResult<DeliverymanTripListItem>>;
