using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.Application.Commands;

public static class OrderCommands
{
    public record CreateOrderCommand(OrderCreateDto CreateDto) : IRequest<Result>;
    public record UpdateOrderCommand() : IRequest<int>;
    public record DeleteOrderCommand() : IRequest<int>;
    public record ChangeOrderStatusCommand(int Id, int OrderStatusId) : IRequest<Result>;
}
