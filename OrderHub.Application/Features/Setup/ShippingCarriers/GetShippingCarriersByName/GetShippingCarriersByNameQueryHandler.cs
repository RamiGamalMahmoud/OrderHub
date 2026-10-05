using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class GetShippingCarriersByNameQueryHandler(IShippingCarrierReader reader) : IRequestHandler<GetShippingCarriersByNameQuery, IEnumerable<ShippingCarrierListItem>>
{
    public Task<IEnumerable<ShippingCarrierListItem>> Handle(GetShippingCarriersByNameQuery request, CancellationToken cancellationToken) =>
        reader.GetShippingCarriersByNameAsync(request.SearchTerm, request.Take, cancellationToken);
}
