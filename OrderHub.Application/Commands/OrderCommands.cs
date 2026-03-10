using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.Application.Commands;

public static class OrderCommands
{
    public record CreateOrderCommand(OrderCreateDto CreateDto) : IRequest<Result<int>>;
    public record UpdateOrderCommand() : IRequest<int>;
    public record DeleteOrderCommand() : IRequest<int>;
    public record BroadcastOrderStatusCommand(int OrderId) : IRequest;
    public record ChangeOrderStatusCommand(int OrderId, OrderStatus OrderStatus) : IRequest<Result>;
}
