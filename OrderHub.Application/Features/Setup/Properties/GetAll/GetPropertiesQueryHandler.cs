using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.GetAll;

internal sealed class GetPropertiesQueryHandler(IPropertyReader reader) : IRequestHandler<GetPropertiesQuery, IEnumerable<PropertyListItem>>
{
    public async Task<IEnumerable<PropertyListItem>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        return await reader.GetAllAsync(cancellationToken);
    }
}
