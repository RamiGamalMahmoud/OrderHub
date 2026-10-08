using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

[RequirePermission(PermissionFeature.ShippingCarriers, PermissionLevel.Read)]
public record GetShippingCarriersByNameQuery(string SearchTerm = null, int Take = 20) : IRequest<IEnumerable<ShippingCarrierListItem>>;
