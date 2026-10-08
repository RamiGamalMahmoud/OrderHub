using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Setup.ShippingCarriers;

internal sealed class ShippingCarrierReader(AppDbContextFactory contextFactory) : IShippingCarrierReader
{
    public async Task<ShippingCarrierEdit> GetShippingCarrierForEditAsync(int id, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();

        return await context.ShippingCarriers.AsNoTracking()
            .Where(carrier => carrier.Id == id)
            .Select(carrier => new ShippingCarrierEdit(
                carrier.Id,
                carrier.Name.Value,
                carrier.ShippingCost.Value,
                carrier.Phone != null ? carrier.Phone.Number.CountryCode : "",
                carrier.Phone != null ? carrier.Phone.Number.NationalNumber : "",
                carrier.Address != null ? carrier.Address.City.Id : 0,
                carrier.Address != null ? carrier.Address.Street : ""))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShippingCarrierListItem>> GetShippingCarriersAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();

        return await Project(context.ShippingCarriers.AsNoTracking()
                .OrderBy(carrier => carrier.Name.Value)
                .ThenBy(carrier => carrier.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShippingCarrierListItem>> GetShippingCarriersByNameAsync(
        string searchTerm,
        int take,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();
        var query = context.ShippingCarriers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();
            query = query.Where(carrier => carrier.Name.Value.Contains(term) ||
                (carrier.Phone != null && (carrier.Phone.Number.CountryCode + " - " + carrier.Phone.Number.NationalNumber).Contains(term)) ||
                (carrier.Address != null && (carrier.Address.Street.Contains(term) || carrier.Address.City.Name.Value.Contains(term))));
        }

        return await Project(query.OrderBy(carrier => carrier.Name.Value).ThenBy(carrier => carrier.Id).Take(take))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShippingCarrierListItem> GetShippingCarrierByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();

        return await Project(context.ShippingCarriers.AsNoTracking().Where(carrier => carrier.Id == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<ShippingCarrierListItem> Project(IQueryable<ShippingCarrier> query) =>
        query.Select(carrier => new ShippingCarrierListItem(
            carrier.Id,
            carrier.Name.Value,
            carrier.ShippingCost.Value,
            carrier.Phone != null ? carrier.Phone.Number.CountryCode + " - " + carrier.Phone.Number.NationalNumber : "",
            carrier.Address != null ? carrier.Address.City.Name.Value + " - " + carrier.Address.Street : ""));

    public async Task<IEnumerable<ShippingCarrierInfo>> GetAllShippingCarriersInfoAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();

        return await context.ShippingCarriers.AsNoTracking()
            .Select(carrier => new ShippingCarrierInfo(carrier.Id, carrier.Name.Value))
            .ToListAsync(cancellationToken);
    }
}
