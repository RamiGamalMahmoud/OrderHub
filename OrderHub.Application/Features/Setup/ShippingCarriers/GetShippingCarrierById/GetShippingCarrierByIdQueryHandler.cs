using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class GetShippingCarrierByIdQueryHandler(IShippingCarrierReader reader) : IRequestHandler<GetShippingCarrierByIdQuery, ShippingCarrierListItem>
{
    public Task<ShippingCarrierListItem> Handle(GetShippingCarrierByIdQuery request, CancellationToken cancellationToken) =>
        reader.GetShippingCarrierByIdAsync(request.Id, cancellationToken);
}
