using OrderHub.Application.Common.Lookups;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetEditorLookups;

public record OrderEditorLookups(
    IReadOnlyList<ClientLookup> Clients,
    IReadOnlyList<PaymentMethodLookup> PaymentMethods,
    IReadOnlyList<DeliverymanLookup> Deliverymen,
    IReadOnlyList<ShippingCarrierLookup> ShippingCarriers);
