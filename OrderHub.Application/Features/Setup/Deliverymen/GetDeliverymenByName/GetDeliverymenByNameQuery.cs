using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

[RequirePermission(PermissionFeature.Deliverymen, PermissionLevel.Read)]
public record GetDeliverymenByNameQuery(string SearchTerm = null, int Take = 20) : IRequest<IEnumerable<DeliverymanListItem>>;
