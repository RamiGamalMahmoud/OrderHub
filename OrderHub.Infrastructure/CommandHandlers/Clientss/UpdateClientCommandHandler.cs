using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ClienCommands;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.Infrastructure.CommandHandlers.Clientss;

internal class UpdateClientCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<UpdateClientCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        ClientUpdateDto dto = request.ClientUpdateDto;
        
        City newCity = await appDbContext.Cities.FindAsync(dto.CityId);

        Client client = await appDbContext
            .Clients
            .Include(c => c.Address).ThenInclude(a => a.City)
            .Include(c => c.Phone)
            .Where(c => c.Id == dto.Id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        client.UpdateName(dto.Name);
        client.UpdatePhone(dto.CountryCode, dto.PhoneNumber);
        client.UpdateAddress(newCity, dto.Street);
        await Services.OutboxRecipientPhoneUpdater.UpdateClientPhoneAsync(
            appDbContext,
            client.Id,
            client.Phone.Number.FullNumber,
            cancellationToken);

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
