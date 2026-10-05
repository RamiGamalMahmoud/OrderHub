using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.Deliverymen;

internal sealed class DeliverymanRepository(AppDbContext context) : IDeliverymanRepository
{
    public Task<bool> IsUsedByOrdersAsync(int id, CancellationToken cancellationToken) =>
        context.Orders.AnyAsync(order => order.DeliverymanId == id, cancellationToken);

    public Task<bool> IsUsedByOrderDeliveryStepsAsync(int id, CancellationToken cancellationToken) =>
        context.OrderDeliverySteps.AnyAsync(step => step.DeliverymanId == id, cancellationToken);

    public async Task<Deliveryman> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Deliverymen.FindAsync([id], cancellationToken);

    public void Add(Deliveryman entity) => context.Deliverymen.Add(entity);

    public void Remove(Deliveryman entity) => context.Deliverymen.Remove(entity);
}
