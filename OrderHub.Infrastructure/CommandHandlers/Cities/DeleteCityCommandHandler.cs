using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CityCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Cities;

internal class DeleteCityCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteCityCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteCityCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        City city = await appDbContext.Cities.FindAsync([request.Id], cancellationToken);
        if (city is null)
        {
            return Result.Failure("المدينة غير موجودة.");
        }

        if (await appDbContext.Addresses.AnyAsync(address => address.City.Id == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المدينة لأنها مرتبطة بعناوين.");
        }

        if (await appDbContext.Deliverymen.AnyAsync(deliveryman => deliveryman.City.Id == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المدينة لأنها مرتبطة بمناديب.");
        }

        appDbContext.Cities.Remove(city);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure($"تعذر حذف المدينة ({city.Name.Value}).");
        }
    }
}
