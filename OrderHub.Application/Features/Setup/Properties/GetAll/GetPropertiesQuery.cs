using MediatR;
using OrderHub.Application.Interfaces.Stores;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.GetAll;

public sealed record GetPropertiesQuery : IRequest<IEnumerable<PropertyListDto>>;

internal sealed class GetPropertiesQueryHandler(IPropertyStore propertyStore) : IRequestHandler<GetPropertiesQuery, IEnumerable<PropertyListDto>>
{
    public async Task<IEnumerable<PropertyListDto>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        return await propertyStore.GetAllAsync(cancellationToken);
    }
}
