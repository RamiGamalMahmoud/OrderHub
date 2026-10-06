using MediatR;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class GetShippingCarrierEditorCitiesHandler(ICityReader reader)
    : IRequestHandler<GetShippingCarrierEditorCitiesQuery, IEnumerable<CityInfo>>
{
    public Task<IEnumerable<CityInfo>> Handle(
        GetShippingCarrierEditorCitiesQuery request,
        CancellationToken cancellationToken) =>
        reader.GetCitiesInfoAsync(cancellationToken);
}
