using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.DeliverymanCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Deliverymen;

internal class CreateDeliverymanCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateDeliverymanCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(CreateDeliverymanCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        City city = await appDbContext.Cities.FindAsync(request.Deliveryman.CityId);
        Deliveryman deliveryman = new Deliveryman(request.Deliveryman.Name, city, request.Deliveryman.PhoneNumber);

        appDbContext.Deliverymen.Add(deliveryman);
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
