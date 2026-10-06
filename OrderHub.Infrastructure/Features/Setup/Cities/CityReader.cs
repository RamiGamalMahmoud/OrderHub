using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.Setup.Cities;

internal sealed class CityReader(AppDbContextFactory contextFactory) : ICityReader
{
    public async Task<IEnumerable<CityListItem>> GetAllCitiesAsync(CancellationToken cancellationToken)
    {
        await using AppDbContext context = contextFactory.CreateDbContext();

        return await context.Cities
            .AsNoTracking()
            .OrderBy(city => city.Name.Value)
            .ThenBy(city => city.Id)
            .Select(city => new CityListItem(city.Id, city.Name.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<CityUpdateInput> GetCityForEditAsync(
        int id,
        CancellationToken cancellationToken)
    {
        await using AppDbContext context = contextFactory.CreateDbContext();

        return await context.Cities
            .AsNoTracking()
            .Where(city => city.Id == id)
            .Select(city => new CityUpdateInput(city.Id, city.Name.Value))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<CityInfo>> GetCitiesInfoAsync(CancellationToken cancellationToken)
    {
        await using AppDbContext context = contextFactory.CreateDbContext();

        return await context.Cities
            .AsNoTracking()
            .Select(city => new CityInfo(city.Id, city.Name.Value))
            .ToListAsync(cancellationToken);
    }
}
