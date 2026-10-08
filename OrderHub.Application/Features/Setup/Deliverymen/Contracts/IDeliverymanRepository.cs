using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Deliverymen.Contracts;

public interface IDeliverymanRepository
{
    Task<bool> IsUsedByOrdersAsync(int id, CancellationToken cancellationToken);
    Task<bool> IsUsedByOrderDeliveryStepsAsync(int id, CancellationToken cancellationToken);
    Task<Deliveryman> FindAsync(int id, CancellationToken cancellationToken);
    void Add(Deliveryman entity);
    void Remove(Deliveryman entity);
}
