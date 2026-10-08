using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

internal sealed class GetCitiesInfoQueryHandler(ICityReader reader) : IRequestHandler<GetCitiesInfoQuery, IEnumerable<CityInfo>>
{
    public Task<IEnumerable<CityInfo>> Handle(GetCitiesInfoQuery request, CancellationToken cancellationToken) =>
        reader.GetCitiesInfoAsync(cancellationToken);
}
