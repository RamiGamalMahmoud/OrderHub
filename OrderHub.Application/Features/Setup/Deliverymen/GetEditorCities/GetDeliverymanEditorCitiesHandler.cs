using MediatR;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class GetDeliverymanEditorCitiesHandler(ICityReader reader)
    : IRequestHandler<GetDeliverymanEditorCitiesQuery, IEnumerable<CityInfo>>
{
    public Task<IEnumerable<CityInfo>> Handle(
        GetDeliverymanEditorCitiesQuery request,
        CancellationToken cancellationToken) =>
        reader.GetCitiesInfoAsync(cancellationToken);
}
