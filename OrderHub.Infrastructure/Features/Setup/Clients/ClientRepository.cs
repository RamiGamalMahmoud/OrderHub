using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.Clients;

internal sealed class ClientRepository(AppDbContext context) : IClientRepository
{
    public async Task<bool> HasBusinessReferencesAsync(int id, CancellationToken cancellationToken)
    {
        if (await context.Orders.AnyAsync(order => order.ClientId == id, cancellationToken))
        {
            return true;
        }

        // Drafts contain a serialized Draft envelope, not a relational Client foreign key.
        if (context.Database.IsNpgsql())
        {
            return await context.DraftRecords.FromSqlInterpolated(
                $"SELECT * FROM order_drafts WHERE data #>> '{{Data,ClientId}}' = {id.ToString(System.Globalization.CultureInfo.InvariantCulture)}")
                .AsNoTracking().AnyAsync(cancellationToken);
        }

        return await context.DraftRecords.FromSqlInterpolated(
            $"SELECT * FROM order_drafts WHERE json_valid(data) AND json_extract(data, '$.Data.ClientId') = {id}")
            .AsNoTracking().AnyAsync(cancellationToken);
    }

    public async Task PrepareContactUpdateAsync(
        Client client,
        ClientInput input,
        City city,
        CancellationToken cancellationToken)
    {
        bool phoneChanged = client.Phone.Number.NationalNumber != input.PhoneNumber.Trim() ||
            client.Phone.Number.CountryCode != input.CountryCode.Trim();
        bool addressChanged = client.Address.Street != input.Street.Trim() || client.Address.City.Id != city.Id;

        if (phoneChanged && (
            await context.Clients.AnyAsync(other => other.Id != client.Id && other.Phone.Id == client.Phone.Id, cancellationToken) ||
            await context.Suppliers.AnyAsync(other => other.Phone.Id == client.Phone.Id, cancellationToken) ||
            await context.ShippingCarriers.AnyAsync(other => other.Phone.Id == client.Phone.Id, cancellationToken)))
        {
            client.ReplacePhone(Phone.Create(input.PhoneNumber.Trim(), input.CountryCode.Trim()).Value);
        }

        if (addressChanged && (
            await context.Clients.AnyAsync(other => other.Id != client.Id && other.Address.Id == client.Address.Id, cancellationToken) ||
            await context.Suppliers.AnyAsync(other => other.Address.Id == client.Address.Id, cancellationToken) ||
            await context.ShippingCarriers.AnyAsync(other => other.Address.Id == client.Address.Id, cancellationToken)))
        {
            client.ReplaceAddress(Address.Create(input.Street.Trim(), city).Value);
        }
    }

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
