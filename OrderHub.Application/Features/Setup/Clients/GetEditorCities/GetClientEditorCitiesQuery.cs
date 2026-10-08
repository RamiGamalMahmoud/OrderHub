using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Clients;

[RequirePermission(PermissionFeature.Clients, PermissionLevel.Write)]
public sealed record GetClientEditorCitiesQuery : IRequest<IEnumerable<CityInfo>>;
