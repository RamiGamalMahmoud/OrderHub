using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

[RequirePermission(PermissionFeature.ShippingCarriers, PermissionLevel.Write)]
public sealed record GetShippingCarrierEditorCitiesQuery : IRequest<IEnumerable<CityInfo>>;
