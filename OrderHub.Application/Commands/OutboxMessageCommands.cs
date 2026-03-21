using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Commands;

public static class OutboxMessageCommands
{
    public record ResendOutboxMessageCommand(int MessageId) : IRequest<Result>;
}
