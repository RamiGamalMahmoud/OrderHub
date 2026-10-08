using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

[RequirePermission(PermissionFeature.Deliverymen, PermissionLevel.Read)]
public record GetDeliverymanByIdQuery(int Id) : IRequest<DeliverymanListItem>;
