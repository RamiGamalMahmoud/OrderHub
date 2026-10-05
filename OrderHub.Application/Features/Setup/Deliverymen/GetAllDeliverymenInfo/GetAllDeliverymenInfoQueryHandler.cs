using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class GetAllDeliverymenInfoQueryHandler(IDeliverymanReader reader) : IRequestHandler<GetAllDeliverymenInfoQuery, IEnumerable<DeliverymanInfo>>
{
    public Task<IEnumerable<DeliverymanInfo>> Handle(GetAllDeliverymenInfoQuery request, CancellationToken cancellationToken) =>
        reader.GetAllDeliverymenInfoAsync(cancellationToken);
}
