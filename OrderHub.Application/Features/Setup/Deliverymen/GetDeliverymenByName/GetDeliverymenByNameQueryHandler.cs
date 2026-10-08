using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class GetDeliverymenByNameQueryHandler(IDeliverymanReader reader) : IRequestHandler<GetDeliverymenByNameQuery, IEnumerable<DeliverymanListItem>>
{
    public Task<IEnumerable<DeliverymanListItem>> Handle(GetDeliverymenByNameQuery request, CancellationToken cancellationToken) =>
        reader.GetDeliverymenByNameAsync(request.SearchTerm, request.Take, cancellationToken);
}
