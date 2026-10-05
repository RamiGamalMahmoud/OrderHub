using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class GetAllDeliverymenListQueryHandler(IDeliverymanReader reader) : IRequestHandler<GetAllDeliverymenListQuery, IEnumerable<DeliverymanListItem>>
{
    public Task<IEnumerable<DeliverymanListItem>> Handle(GetAllDeliverymenListQuery request, CancellationToken cancellationToken) =>
        reader.GetAllDeliverymenListAsync(cancellationToken);
}
