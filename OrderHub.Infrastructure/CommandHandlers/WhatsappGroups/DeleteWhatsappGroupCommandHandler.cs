using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.WhatsappGroupCommands;

namespace OrderHub.Infrastructure.CommandHandlers.WhatsappGroups;

internal class DeleteWhatsappGroupCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteWhatsappGroupCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteWhatsappGroupCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        WhatsappGroup whatsappGroup = await appDbContext.WhatsappGroups.FindAsync(new object[] { request.Id }, cancellationToken);
        if (whatsappGroup == null)
        {
            return Result.Failure("مجموعة الواتساب غير موجودة.");
        }

        try
        {
            appDbContext.WhatsappGroups.Remove(whatsappGroup);
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure("تعذر حذف مجموعة الواتساب.");
        }
    }
}
