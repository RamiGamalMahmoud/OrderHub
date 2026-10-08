using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class GetShippingCarriersQueryHandler(IShippingCarrierReader reader) : IRequestHandler<GetShippingCarriersQuery, IEnumerable<ShippingCarrierListItem>>
{
    public Task<IEnumerable<ShippingCarrierListItem>> Handle(GetShippingCarriersQuery request, CancellationToken cancellationToken) =>
        reader.GetShippingCarriersAsync(cancellationToken);
}
