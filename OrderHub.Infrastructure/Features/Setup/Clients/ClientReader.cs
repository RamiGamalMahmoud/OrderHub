using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.Setup.Clients;

internal sealed class ClientReader(AppDbContextFactory contextFactory) : IClientReader
{
    public async Task<PagedResult<ClientListItem>> GetAllClientsPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        var query = appDbContext.Clients.AsNoTracking()
            .OrderBy(c => c.Name.Value)
            .Select(c => new ClientListItem(
                c.Id,
                c.Name.Value,
                $"{c.Address.City.Name.Value} - {c.Address.Street}",
                c.Phone.Number.FullNumber,
                c.Location));

        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<IEnumerable<ClientListItem>> GetAllClientsAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext
            .Clients.AsNoTracking()
            .Select(c => new ClientListItem(
                c.Id,
                c.Name.Value,
                $"{c.Address.City.Name.Value} - {c.Address.Street}",
                c.Phone.Number.FullNumber,
                c.Location)
            )
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<ClientInput> GetClientEditAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        var result = await appDbContext
            .Clients.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(client => new ClientInput(
                client.Name.Value,
                client.Address.Street,
                client.Address.City.Id,
                client.Phone == null ? null : client.Phone.Number.NationalNumber,
                client.Phone == null ? null : client.Phone.Number.CountryCode,
                client.Location))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        // Preserve the existing missing-client exception rather than changing caller semantics.
        return result ?? throw new System.NullReferenceException();
    }

    public async Task<IEnumerable<ClientListItem>> GetClientsByNameAsync(string searchTerm, int take, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        IQueryable<Domain.Models.Client> query = appDbContext.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();
            query = query.Where(client =>
                client.Name.Value.Contains(term) ||
                (client.Phone.Number.CountryCode + " - " + client.Phone.Number.NationalNumber).Contains(term) ||
                client.Address.Street.Contains(term) ||
                client.Address.City.Name.Value.Contains(term));
        }

        return await query
            .OrderBy(client => client.Name.Value)
            .Take(take)
            .Select(client => new ClientListItem(
                client.Id,
                client.Name.Value,
                $"{client.Address.City.Name.Value} - {client.Address.Street}",
                client.Phone.Number.FullNumber, client.Location))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientListItem> GetClientByIdAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext.Clients.AsNoTracking()
            .Where(client => client.Id == id)
            .Select(client => new ClientListItem(
                client.Id,
                client.Name.Value,
                $"{client.Address.City.Name.Value} - {client.Address.Street}",
                client.Phone.Number.FullNumber,
                client.Location))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ClientInfo>> GetClientsInfoAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext
            .Clients.AsNoTracking()
            .Select(c => new ClientInfo(c.Id, c.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
