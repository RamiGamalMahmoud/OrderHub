using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class GetDeliverymanForEditQueryHandler(IDeliverymanReader reader) : IRequestHandler<GetDeliverymanForEditQuery, DeliverymanInput>
{
    public Task<DeliverymanInput> Handle(GetDeliverymanForEditQuery request, CancellationToken cancellationToken) =>
        reader.GetDeliverymanForEditAsync(request.Id, cancellationToken);
}
