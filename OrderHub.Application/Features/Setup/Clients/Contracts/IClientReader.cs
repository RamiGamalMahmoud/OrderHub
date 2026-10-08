using System.Collections.Generic;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Clients;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Application.Features.Setup.Clients.Contracts;

public interface IClientReader
{
    Task<PagedResult<ClientListItem>> GetAllClientsPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IEnumerable<ClientListItem>> GetAllClientsAsync(CancellationToken cancellationToken);
    Task<ClientInput> GetClientEditAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<ClientListItem>> GetClientsByNameAsync(string searchTerm, int take, CancellationToken cancellationToken);
    Task<ClientListItem> GetClientByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<ClientInfo>> GetClientsInfoAsync(CancellationToken cancellationToken);
}
