using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Deliverymen;

[RequirePermission(PermissionFeature.Deliverymen, PermissionLevel.Write)]
public sealed record GetDeliverymanEditorCitiesQuery : IRequest<IEnumerable<CityInfo>>;
