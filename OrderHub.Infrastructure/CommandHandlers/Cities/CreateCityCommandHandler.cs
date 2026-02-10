using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.CityCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Cities;

internal class CreateCityCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateCityCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(CreateCityCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        City city = new City(request.CityCreateDto.Name);
        appDbContext.Cities.Add(city);
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
