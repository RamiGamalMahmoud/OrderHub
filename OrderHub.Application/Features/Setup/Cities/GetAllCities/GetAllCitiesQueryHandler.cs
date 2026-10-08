using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Features.Setup.Cities;

namespace OrderHub.Application.Features.Setup.Cities;

internal sealed class GetAllCitiesQueryHandler(ICityReader reader) : IRequestHandler<GetAllCitiesQuery, IEnumerable<CityListItem>>
{
    public Task<IEnumerable<CityListItem>> Handle(GetAllCitiesQuery request, CancellationToken cancellationToken) =>
        reader.GetAllCitiesAsync(cancellationToken);
}
