using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.Orders.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Orders;

internal sealed class OrderEditorLookupReader(AppDbContextFactory contextFactory) : IOrderEditorLookupReader
{
    public async Task<IEnumerable<ClientLookup>> GetClients(CancellationToken cancellationToken = default)
    {
        using var context = contextFactory.CreateDbContext();
        return await context.Clients.AsNoTracking()
            .Select(client => new ClientLookup(client.Id, client.Name.Value, client.Phone.Number.FullNumber, client.Address.FullAddress))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentMethodLookup>> GetPaymentMethods(CancellationToken cancellationToken = default)
    {
        using var context = contextFactory.CreateDbContext();
        return await context.PaymentMethods.AsNoTracking()
            .Select(method => new PaymentMethodLookup(method.Id, method.DisplayName, method.DisplayName, method.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeliverymanLookup>> GetDeliveryMethods(CancellationToken cancellationToken = default)
    {
        using var context = contextFactory.CreateDbContext();
        return await context.Deliverymen.AsNoTracking()
            .Select(deliveryman => new DeliverymanLookup(deliveryman.Id, deliveryman.Name.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShippingCarrierLookup>> GetShippingCarriers(CancellationToken cancellationToken = default)
    {
        using var context = contextFactory.CreateDbContext();
        return await context.ShippingCarriers.AsNoTracking()
            .Select(carrier => new ShippingCarrierLookup(carrier.Id, carrier.Name.Value))
            .ToListAsync(cancellationToken);
    }
}
