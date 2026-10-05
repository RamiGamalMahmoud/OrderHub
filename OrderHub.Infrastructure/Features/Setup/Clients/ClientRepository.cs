using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.Clients;

internal sealed class ClientRepository(AppDbContext context) : IClientRepository
{
    public Task<bool> IsUsedByOrdersAsync(int id, CancellationToken cancellationToken) =>
        context.Orders.AnyAsync(order => order.ClientId == id, cancellationToken);

    public Task<Client> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        context
            .Clients
            .Include(c => c.Address).ThenInclude(a => a.City)
            .Include(c => c.Phone)
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

    public async Task<Client> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Clients.FindAsync([id], cancellationToken);

    public void Add(Client entity) => context.Clients.Add(entity);

    public void Remove(Client entity) => context.Clients.Remove(entity);
}
