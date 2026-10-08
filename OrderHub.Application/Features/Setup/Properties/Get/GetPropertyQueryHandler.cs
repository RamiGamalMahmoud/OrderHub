using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Get;

internal sealed class GetPropertyQueryHandler(IPropertyReader reader) : IRequestHandler<GetPropertyQuery, PropertyDetails>
{
    public async Task<PropertyDetails> Handle(
        GetPropertyQuery request,
        CancellationToken cancellationToken)
    {
        var property = await reader.GetByIdAsync(
            request.Id,
            cancellationToken);

        return property;
    }
}
