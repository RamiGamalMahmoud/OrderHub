using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

[RequirePermission(PermissionFeature.ShippingCarriers, PermissionLevel.Read)]
public record GetAllShippingCarriersInfoQuery : IRequest<IEnumerable<ShippingCarrierInfo>>;
