using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.DeliverymanDtos;

namespace OrderHub.Application.Commands;

public static class DeliverymanCommands
{
    public record CreateDeliverymanCommand(DeliverymanCreateDto DeliverymanCreateDto) : IRequest<Result>;
    public record UpdateDeliverymanCommand(DeliverymanUpdateDto DeliverymanUpdateDto) : IRequest<Result>;
    public record DeleteDeliverymanCommand(int Id) : IRequest<Result>;
}
