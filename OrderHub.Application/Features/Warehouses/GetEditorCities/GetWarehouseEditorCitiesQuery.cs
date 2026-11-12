using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Warehouses;

[RequirePermission(PermissionFeature.Warehouses, PermissionLevel.Write)]
public sealed record GetWarehouseEditorCitiesQuery : IRequest<IEnumerable<CityInfo>>;
