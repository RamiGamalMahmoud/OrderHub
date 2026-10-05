using OrderHub.Application.Common.Lookups;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Contracts;

public interface IOrderEditorLookupReader
{
    Task<IEnumerable<ClientLookup>> GetClients(CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentMethodLookup>> GetPaymentMethods(CancellationToken cancellationToken = default);
    Task<IEnumerable<DeliverymanLookup>> GetDeliveryMethods(CancellationToken cancellationToken = default);
    Task<IEnumerable<ShippingCarrierLookup>> GetShippingCarriers(CancellationToken cancellationToken = default);
}
