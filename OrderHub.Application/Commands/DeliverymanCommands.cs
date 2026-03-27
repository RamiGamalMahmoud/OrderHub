using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.Application.Commands;

public static class DeliverymanCommands
{
    public record CreateDeliverymanCommand(DeliverymanFormDto Deliveryman) : IRequest<Result>;
    public record UpdateDeliverymanCommand(int Id, DeliverymanFormDto Deliveryman) : IRequest<Result>;
    public record DeleteDeliverymanCommand(int Id) : IRequest<Result>;
}
