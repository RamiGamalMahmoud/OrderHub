using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Cities.Contracts;

public interface ICityRepository
{
    Task<bool> IsUsedByAddressesAsync(int id, CancellationToken cancellationToken);
    Task<bool> IsUsedByDeliverymenAsync(int id, CancellationToken cancellationToken);
    Task<City> FindAsync(int id, CancellationToken cancellationToken);
    void Add(City entity);
    void Remove(City entity);
}
