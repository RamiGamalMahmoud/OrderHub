using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

public record UpdateDeliverymanCommand(int Id, DeliverymanInput Deliveryman) : IRequest<Result>;
