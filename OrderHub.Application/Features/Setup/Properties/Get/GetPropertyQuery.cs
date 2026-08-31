using MediatR;
using OrderHub.Application.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Get;

public sealed record GetPropertyQuery(int Id) : IRequest<PropertyDetailsDto>;

internal sealed class GetPropertyQueryHandler(IPropertyStore propertyStore) : IRequestHandler<GetPropertyQuery, PropertyDetailsDto>
{
    public async Task<PropertyDetailsDto> Handle(
        GetPropertyQuery request,
        CancellationToken cancellationToken)
    {
        var property = await propertyStore.GetByIdAsync(
            request.Id,
            cancellationToken);

        return property;
    }
}
