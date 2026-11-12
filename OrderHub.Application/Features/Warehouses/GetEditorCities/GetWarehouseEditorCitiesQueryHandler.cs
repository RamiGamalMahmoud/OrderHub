using MediatR;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Warehouses;

internal sealed class GetWarehouseEditorCitiesQueryHandler(ICityReader reader)
    : IRequestHandler<GetWarehouseEditorCitiesQuery, IEnumerable<CityInfo>>
{
    public Task<IEnumerable<CityInfo>> Handle(
        GetWarehouseEditorCitiesQuery request,
        CancellationToken cancellationToken) =>
        reader.GetCitiesInfoAsync(cancellationToken);
}
