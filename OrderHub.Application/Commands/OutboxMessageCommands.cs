using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Commands;

public static class OutboxMessageCommands
{
    public record ResendOutboxMessageCommand(int MessageId) : IRequest<Result>;
    public record DeleteOutboxMessageCommand(int MessageId) : IRequest<Result>;
}
