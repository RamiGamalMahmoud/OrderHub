using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CityCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Cities;

internal class UpdateCityCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<UpdateCityCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        City city = await appDbContext.Cities.FindAsync([request.CityUpdateDto.Id], cancellationToken);
        if (city is null)
        {
            return Result.Failure("المدينة غير موجودة.");
        }

        city.Rename(request.CityUpdateDto.Name);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure($"تعذر تحديث المدينة ({request.CityUpdateDto.Name}).");
        }
    }
}
