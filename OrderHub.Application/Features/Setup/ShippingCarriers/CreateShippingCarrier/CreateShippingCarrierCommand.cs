using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

[RequirePermission(PermissionFeature.ShippingCarriers, PermissionLevel.Write)]
public record CreateShippingCarrierCommand(ShippingCarrierCreateInput Dto) : IRequest<Result>;
