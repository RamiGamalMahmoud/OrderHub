using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Features.Setup.Cities;

namespace OrderHub.Application.Features.Setup.Cities;

internal sealed class GetCityForEditQueryHandler(ICityReader reader) : IRequestHandler<GetCityForEditQuery, CityUpdateInput>
{
    public Task<CityUpdateInput> Handle(GetCityForEditQuery request, CancellationToken cancellationToken) =>
        reader.GetCityForEditAsync(request.Id, cancellationToken);
}
