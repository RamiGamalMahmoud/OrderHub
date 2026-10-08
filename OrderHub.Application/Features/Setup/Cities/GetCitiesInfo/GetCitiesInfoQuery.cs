using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

[RequirePermission(PermissionFeature.Cities, PermissionLevel.Read)]
public record GetCitiesInfoQuery : IRequest<IEnumerable<CityInfo>>;
