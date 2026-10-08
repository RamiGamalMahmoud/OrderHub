using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

public interface IShippingCarrierRepository
{
    Task<bool> IsUsedByOrdersAsync(int id, CancellationToken cancellationToken);
    Task<bool> IsUsedByOrderDeliveryStepsAsync(int id, CancellationToken cancellationToken);
    Task<ShippingCarrier> GetForUpdateAsync(int id, CancellationToken cancellationToken);
    Task<ShippingCarrier> FindAsync(int id, CancellationToken cancellationToken);
    void Add(ShippingCarrier entity);
    void Remove(ShippingCarrier entity);
}
