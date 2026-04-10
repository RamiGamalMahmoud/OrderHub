using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OutboxMessageCommands;

namespace OrderHub.Infrastructure.CommandHandlers.OutboxMessages;

internal class ResendOutboxMessageCommandHandler(
    AppDbContextFactory appDbContextFactory,
    IMessageService messageService,
    IMessenger messenger)
    : IRequestHandler<ResendOutboxMessageCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;
    private readonly IMessageService _messageService = messageService;
    private readonly IMessenger _messenger = messenger;

    public async Task<Result> Handle(ResendOutboxMessageCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        OutboxMessage message = await appDbContext.OutboxMessages
            .Include(m => m.Recipient)
            .SingleOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

        if (message is null)
        {
            return Result.Failure("الرسالة غير موجودة.");
        }

        message.Status = OutboxMessageStatus.Pending;
        message.RetryCount = (message.RetryCount ?? 0) + 1;
        message.LastAttemptAt = null;
        message.SentAt = null;

        await appDbContext.SaveChangesAsync(cancellationToken);

        _messageService.QueueMessage(message);
        _messenger.Send(new Application.Messages.OutboxMessages.MessageStatusChangedMessage(
            message.Id,
            message.Status,
            message.OrderId,
            message.RecipientType));

        return Result.Success();
    }
}
