using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

[RequirePermission(PermissionFeature.Deliverymen, PermissionLevel.Write)]
public record CreateDeliverymanCommand(DeliverymanInput Deliveryman) : IRequest<Result>;
