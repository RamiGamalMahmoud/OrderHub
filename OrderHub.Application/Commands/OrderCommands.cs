using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Commands;

public static class OrderCommands
{
    public record BroadcastOrderStatusCommand(int OrderId, RecipientType? RecipientType = null) : IRequest<Result>;
    public record ChangeOrderStatusCommand(int OrderId, OrderStatus OrderStatus) : IRequest<Result>;
    public record ChangePaymentMethodCommand(int OrderId, int PaymentMethodId) : IRequest<Result>;
}
