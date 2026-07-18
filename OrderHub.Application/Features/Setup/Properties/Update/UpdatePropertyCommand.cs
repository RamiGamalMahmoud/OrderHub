using MediatR;
using OrderHub.Application.Interfaces.Stores;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Update;

public sealed record UpdatePropertyCommand(PropertyUpdateDto Dto) : IRequest;

internal sealed class UpdatePropertyCommandHandler(IPropertyStore propertyStore) : IRequestHandler<UpdatePropertyCommand>
{
    public async Task Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        await propertyStore.UpdateAsync(request.Dto, cancellationToken);
        throw new System.NotImplementedException();
    }
}
