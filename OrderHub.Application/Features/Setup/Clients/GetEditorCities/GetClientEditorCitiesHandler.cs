using MediatR;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class GetClientEditorCitiesHandler(ICityReader reader)
    : IRequestHandler<GetClientEditorCitiesQuery, IEnumerable<CityInfo>>
{
    public Task<IEnumerable<CityInfo>> Handle(
        GetClientEditorCitiesQuery request,
        CancellationToken cancellationToken) =>
        reader.GetCitiesInfoAsync(cancellationToken);
}
