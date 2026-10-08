using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class GetShippingCarrierForEditQueryHandler(IShippingCarrierReader reader) : IRequestHandler<GetShippingCarrierForEditQuery, ShippingCarrierEdit>
{
    public Task<ShippingCarrierEdit> Handle(GetShippingCarrierForEditQuery request, CancellationToken cancellationToken) =>
        reader.GetShippingCarrierForEditAsync(request.Id, cancellationToken);
}
