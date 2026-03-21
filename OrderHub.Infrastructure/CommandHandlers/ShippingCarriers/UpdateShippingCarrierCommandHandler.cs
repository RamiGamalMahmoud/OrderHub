using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ShippingCarriersCommands;

namespace OrderHub.Infrastructure.CommandHandlers.ShippingCarriers;

internal class UpdateShippingCarrierCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<UpdateShippingCarrierCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(UpdateShippingCarrierCommand request, CancellationToken cancellationToken)
    {
        AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        ShippingCarrier shippingCarrier = await appDbContext.ShippingCarriers
            .Include(s => s.Address).ThenInclude(a => a.City)
            .Include(s => s.Phone)
            .Where(s => s.Id == request.Dto.Id)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (shippingCarrier is null)
            return Result.Failure("Selected Shipping Carrier not found.");

        City selectedCity = await appDbContext.Cities.FindAsync(request.Dto.CityId);
        if (selectedCity is null)
            return Result.Failure("Selected City not found.");

        shippingCarrier.Rename(request.Dto.Name);
        shippingCarrier.Phone.ChangeNumber(request.Dto.PhoneNumber, request.Dto.CountryCode);
        shippingCarrier.Address.ChangeCity(selectedCity);
        shippingCarrier.Address.ChangeStreet(request.Dto.Street);
        shippingCarrier.ChangeShippingCost(request.Dto.ShippingCost);
        await Services.OutboxRecipientPhoneUpdater.UpdateShippingCarrierPhoneAsync(
            appDbContext,
            shippingCarrier.Id,
            shippingCarrier.Phone.Number.FullNumber,
            cancellationToken);

        try
        {
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch(DbUpdateException)
        {
            return Result.Failure($"Failed to update Shipping Carrier {request.Dto.Name}.");
        }
    }
}
