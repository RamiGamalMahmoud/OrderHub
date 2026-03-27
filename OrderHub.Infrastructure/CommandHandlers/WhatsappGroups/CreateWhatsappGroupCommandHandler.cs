using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.WhatsappGroupCommands;

namespace OrderHub.Infrastructure.CommandHandlers.WhatsappGroups;

internal class CreateWhatsappGroupCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateWhatsappGroupCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(CreateWhatsappGroupCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        WhatsappGroup whatsappGroup = new WhatsappGroup
        {
            GroupName = request.Name,
            GroupType = request.WhatsappGroupType
        };

        appDbContext.WhatsappGroups.Add(whatsappGroup);

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
