using MediatR;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class GetSupplierEditorCitiesHandler(ICityReader reader)
    : IRequestHandler<GetSupplierEditorCitiesQuery, IEnumerable<CityInfo>>
{
    public Task<IEnumerable<CityInfo>> Handle(
        GetSupplierEditorCitiesQuery request,
        CancellationToken cancellationToken) =>
        reader.GetCitiesInfoAsync(cancellationToken);
}
