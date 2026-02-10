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
        Client client = await appDbContext.Clients.FindAsync(request.Id);
        appDbContext.Clients.Remove(client);

        try
        {
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure();
        }
    }
}
