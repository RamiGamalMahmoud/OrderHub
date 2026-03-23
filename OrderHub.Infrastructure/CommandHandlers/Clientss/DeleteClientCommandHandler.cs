using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ClienCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Clientss;

internal class DeleteClientCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteClientCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        Client client = await appDbContext.Clients.FindAsync([request.Id], cancellationToken);
        if (client is null)
        {
            return Result.Failure("العميل غير موجود.");
        }

        if (await appDbContext.Orders.AnyAsync(order => order.ClientId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف العميل لأنه مرتبط بطلبات.");
        }

        if (await appDbContext.ClientRecipients.AnyAsync(recipient => recipient.ClientId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف العميل لأنه مرتبط بسجل الرسائل.");
        }

        appDbContext.Clients.Remove(client);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure("تعذر حذف العميل.");
        }
    }
}
