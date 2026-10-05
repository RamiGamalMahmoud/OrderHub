using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

public record GetAllDeliverymenInfoQuery : IRequest<IEnumerable<DeliverymanInfo>>;
