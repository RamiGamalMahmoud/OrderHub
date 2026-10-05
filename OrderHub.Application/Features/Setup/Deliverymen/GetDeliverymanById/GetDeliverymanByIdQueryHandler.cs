using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class GetDeliverymanByIdQueryHandler(IDeliverymanReader reader) : IRequestHandler<GetDeliverymanByIdQuery, DeliverymanListItem>
{
    public Task<DeliverymanListItem> Handle(GetDeliverymanByIdQuery request, CancellationToken cancellationToken) =>
        reader.GetDeliverymanByIdAsync(request.Id, cancellationToken);
}
