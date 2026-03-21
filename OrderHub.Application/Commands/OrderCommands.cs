using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.Application.Commands;

public static class OrderCommands
{
    public record CreateOrderCommand(OrderCreateDto CreateDto) : IRequest<Result<int>>;
    public record UpdateOrderCommand(OrderUpdateDto UpdateDto) : IRequest<Result>;
    public record DeleteOrderCommand(int OrderId) : IRequest<Result>;
    public record BroadcastOrderStatusCommand(int OrderId, RecipientType? RecipientType = null) : IRequest<Result>;
    public record ChangeOrderStatusCommand(int OrderId, OrderStatus OrderStatus) : IRequest<Result>;
    public record ChangePaymentMethodCommand(int OrderId, int PaymentMethodId) : IRequest<Result>;
}
