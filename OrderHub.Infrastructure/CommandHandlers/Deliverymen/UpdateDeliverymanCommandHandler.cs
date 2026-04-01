using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.DeliverymanCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Deliverymen;

internal class UpdateDeliverymanCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<UpdateDeliverymanCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(UpdateDeliverymanCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        Deliveryman deliveryman = await appDbContext.Deliverymen.FindAsync(request.Id);

        City city = await appDbContext.Cities.FindAsync(request.Deliveryman.CityId);
        WhatsappGroup whatsappGroup = request.Deliveryman.WhatsappGroupId is int whatsappGroupId
            ? await appDbContext.WhatsappGroups.FindAsync(whatsappGroupId)
            : null;

        if(deliveryman is null)
        {
            return Result.Failure();
        }

        deliveryman.Rename(request.Deliveryman.Name);
        deliveryman.PhoneNumber = request.Deliveryman.PhoneNumber;
        deliveryman.ChangeCity(city);
        deliveryman.ChangeWhatsappGroup(whatsappGroup);
        await Services.OutboxRecipientPhoneUpdater.UpdateDeliverymanPhoneAsync(
            appDbContext,
            deliveryman.Id,
            deliveryman.PhoneNumber,
            cancellationToken);

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
