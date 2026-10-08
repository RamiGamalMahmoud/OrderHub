using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

[RequirePermission(PermissionFeature.Deliverymen, PermissionLevel.Read)]
public record GetAllDeliverymenInfoQuery : IRequest<IEnumerable<DeliverymanInfo>>;
