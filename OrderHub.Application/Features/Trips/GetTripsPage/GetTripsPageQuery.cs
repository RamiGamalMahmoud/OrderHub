using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Trips.GetTripsPage;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read)]
public record GetTripsPageQuery(
    TripSearchCriteria Criteria,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<TripListItem>>;
