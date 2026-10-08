using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class GetAllShippingCarriersInfoQueryHandler(IShippingCarrierReader reader) : IRequestHandler<GetAllShippingCarriersInfoQuery, IEnumerable<ShippingCarrierInfo>>
{
    public Task<IEnumerable<ShippingCarrierInfo>> Handle(GetAllShippingCarriersInfoQuery request, CancellationToken cancellationToken) =>
        reader.GetAllShippingCarriersInfoAsync(cancellationToken);
}
