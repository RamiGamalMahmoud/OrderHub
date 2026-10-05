using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Clients.Contracts;

public interface IClientRepository
{
    Task<bool> IsUsedByOrdersAsync(int id, CancellationToken cancellationToken);
    Task<Client> GetForUpdateAsync(int id, CancellationToken cancellationToken);
    Task<Client> FindAsync(int id, CancellationToken cancellationToken);
    void Add(Client entity);
    void Remove(Client entity);
}
