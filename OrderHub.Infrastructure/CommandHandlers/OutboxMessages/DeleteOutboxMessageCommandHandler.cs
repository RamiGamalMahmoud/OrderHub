using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OutboxMessageCommands;

namespace OrderHub.Infrastructure.CommandHandlers.OutboxMessages;

internal class DeleteOutboxMessageCommandHandler(AppDbContextFactory appDbContextFactory)
    : IRequestHandler<DeleteOutboxMessageCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteOutboxMessageCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        OutboxMessage message = await appDbContext.OutboxMessages
            .Include(m => m.Recipient)
            .SingleOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

        if (message is null)
        {
            return Result.Failure("الرسالة غير موجودة.");
        }

        if (message.Recipient is not null)
        {
            appDbContext.Remove(message.Recipient);
        }

        appDbContext.OutboxMessages.Remove(message);
        await appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
