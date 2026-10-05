using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.ShippingCarriers;

internal sealed class ShippingCarrierRepository(AppDbContext context) : IShippingCarrierRepository
{
    public Task<bool> IsUsedByOrdersAsync(int id, CancellationToken cancellationToken) =>
        context.Orders.AnyAsync(order => order.ShippingCarrierId == id, cancellationToken);

    public Task<bool> IsUsedByOrderDeliveryStepsAsync(int id, CancellationToken cancellationToken) =>
        context.OrderDeliverySteps.AnyAsync(step => step.ShippingCarrierId == id, cancellationToken);

    public Task<ShippingCarrier> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        context.ShippingCarriers
            .Include(s => s.Address).ThenInclude(a => a.City)
            .Include(s => s.Phone)
            .Where(s => s.Id == id)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

    public async Task<ShippingCarrier> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.ShippingCarriers.FindAsync([id], cancellationToken);

    public void Add(ShippingCarrier entity) => context.ShippingCarriers.Add(entity);

    public void Remove(ShippingCarrier entity) => context.ShippingCarriers.Remove(entity);
}
