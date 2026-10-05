using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Features.Setup.Cities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.Setup.Cities;

internal sealed class CityReader(AppDbContextFactory contextFactory) : ICityReader
{
    public async Task<IEnumerable<CityListItem>> GetAllCitiesAsync(CancellationToken cancellationToken)
    {
        using (AppDbContext appDbContext = contextFactory.CreateDbContext())
        {
            return await appDbContext
                .Cities.AsNoTracking()
                .Select(c => new CityListItem(c.Id, c.Name.Value))
                .ToListAsync(cancellationToken: cancellationToken);
        }
    }

    public async Task<CityUpdateInput> GetCityForEditAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext
            .Cities.AsNoTracking()
            .Where(city => city.Id == id)
            .Select(city => new CityUpdateInput(city.Id, city.Name.Value))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<CityInfo>> GetCitiesInfoAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext
            .Cities.AsNoTracking()
            .Select(c => new CityInfo(c.Id, c.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
