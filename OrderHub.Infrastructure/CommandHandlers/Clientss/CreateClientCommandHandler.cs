using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ClienCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Clientss;

internal class CreateClientCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateClientCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        City city = await appDbContext.Cities.FindAsync(request.ClientCreateDto.CityId);
        Client client = new Client(
            request.ClientCreateDto.Name,
            request.ClientCreateDto.Street,
            city,
            request.ClientCreateDto.PhoneNumber,
            request.ClientCreateDto.CountryCode);

        appDbContext.Clients.Add(client);

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
