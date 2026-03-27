using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.WhatsappGroupCommands;

namespace OrderHub.Infrastructure.CommandHandlers.WhatsappGroups;

internal class UpdateWhatsappGroupCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<UpdateWhatsappGroupCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(UpdateWhatsappGroupCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        WhatsappGroup whatsappGroup = await appDbContext
            .WhatsappGroups
            .Where(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (whatsappGroup == null)
        {
            return Result.Failure();
        }

        whatsappGroup.GroupName = request.Name;
        whatsappGroup.GroupType = request.WhatsappGroupType;

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure();
        }
    }
}
