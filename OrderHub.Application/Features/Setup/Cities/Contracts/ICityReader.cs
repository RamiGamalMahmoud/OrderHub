using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Features.Setup.Cities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Application.Features.Setup.Cities.Contracts;

public interface ICityReader
{
    Task<IEnumerable<CityListItem>> GetAllCitiesAsync(CancellationToken cancellationToken);
    Task<CityUpdateInput> GetCityForEditAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<CityInfo>> GetCitiesInfoAsync(CancellationToken cancellationToken);
}
