using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.Setup.ShippingCarriers;

internal sealed class ShippingCarrierReader(AppDbContextFactory contextFactory) : IShippingCarrierReader
{
    public async Task<ShippingCarrierEdit> GetShippingCarrierForEditAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext
            .ShippingCarriers.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ShippingCarrierEdit(
                s.Id,
                s.Name.Value,
                s.ShippingCost.Value,
                s.Phone.Number.CountryCode,
                s.Phone.Number.NationalNumber,
                s.Address.City.Id,
                s.Address.Street))
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ShippingCarrierListItem>> GetShippingCarriersByNameAsync(string searchTerm, int take, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        IQueryable<Domain.Models.ShippingCarrier> query = appDbContext.ShippingCarriers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();
            query = query.Where(carrier =>
                carrier.Name.Value.Contains(term) ||
                (carrier.Phone.Number.CountryCode + " - " + carrier.Phone.Number.NationalNumber).Contains(term) ||
                carrier.Address.Street.Contains(term) ||
                carrier.Address.City.Name.Value.Contains(term));
        }

        return await query
            .OrderBy(carrier => carrier.Name.Value)
            .Take(take)
            .Select(carrier => new ShippingCarrierListItem(
                carrier.Id,
                carrier.Name.Value,
                carrier.ShippingCost.Value,
                carrier.Phone.Number.FullNumber,
                $"{carrier.Address.City.Name.Value} - {carrier.Address.Street}"))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShippingCarrierListItem> GetShippingCarrierByIdAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext.ShippingCarriers.AsNoTracking()
            .Where(carrier => carrier.Id == id)
            .Select(carrier => new ShippingCarrierListItem(
                carrier.Id,
                carrier.Name.Value,
                carrier.ShippingCost.Value,
                carrier.Phone.Number.FullNumber,
                $"{carrier.Address.City.Name.Value} - {carrier.Address.Street}"))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShippingCarrierListItem>> GetShippingCarriersAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext
            .ShippingCarriers.AsNoTracking()
            .Select(s => new ShippingCarrierListItem(
                s.Id,
                s.Name.Value,
                s.ShippingCost.Value,
                $"{s.Phone.Number.FullNumber}",
                $"{s.Address.City.Name} - {s.Address.Street}"))
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ShippingCarrierInfo>> GetAllShippingCarriersInfoAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext.ShippingCarriers.AsNoTracking()
            .Select(s => new ShippingCarrierInfo(s.Id, s.Name.Value))
            .ToListAsync(cancellationToken);
    }
}
