using MediatR;
using OrderHub.Application.Features.Orders.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.GetEditorLookups;

internal sealed class GetOrderEditorLookupsHandler(IOrderEditorLookupReader reader)
    : IRequestHandler<GetOrderEditorLookupsQuery, OrderEditorLookups>
{
    public async Task<OrderEditorLookups> Handle(GetOrderEditorLookupsQuery request, CancellationToken cancellationToken)
    {
        var clients = (await reader.GetClients(cancellationToken)).ToArray();
        var paymentMethods = (await reader.GetPaymentMethods(cancellationToken)).ToArray();
        var deliverymen = (await reader.GetDeliveryMethods(cancellationToken)).ToArray();
        var carriers = (await reader.GetShippingCarriers(cancellationToken)).ToArray();
        return new(clients, paymentMethods, deliverymen, carriers);
    }
}
