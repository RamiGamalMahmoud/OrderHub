using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.Setup.Deliverymen;

internal sealed class DeliverymanReader(AppDbContextFactory contextFactory) : IDeliverymanReader
{
    public async Task<IEnumerable<DeliverymanListItem>> GetAllDeliverymenListAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext.Deliverymen.AsNoTracking()
            .Select(d => new DeliverymanListItem(
                d.Id,
                d.Name.Value,
                d.City.Name.Value,
                d.PhoneNumber))
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<DeliverymanInput> GetDeliverymanForEditAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext.Deliverymen.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DeliverymanInput(
                d.Name.Value,
                d.City.Id,
                d.PhoneNumber))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<DeliverymanListItem>> GetDeliverymenByNameAsync(string searchTerm, int take, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        IQueryable<Domain.Models.Deliveryman> query = appDbContext.Deliverymen.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();
            query = query.Where(deliveryman =>
                deliveryman.Name.Value.Contains(term) ||
                deliveryman.PhoneNumber.Contains(term) ||
                deliveryman.City.Name.Value.Contains(term));
        }

        return await query
            .OrderBy(deliveryman => deliveryman.Name.Value)
            .Take(take)
            .Select(deliveryman => new DeliverymanListItem(
                deliveryman.Id,
                deliveryman.Name.Value,
                deliveryman.City.Name.Value,
                deliveryman.PhoneNumber))
            .ToListAsync(cancellationToken);
    }

    public async Task<DeliverymanListItem> GetDeliverymanByIdAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        return await appDbContext.Deliverymen.AsNoTracking()
            .Where(deliveryman => deliveryman.Id == id)
            .Select(deliveryman => new DeliverymanListItem(
                deliveryman.Id,
                deliveryman.Name.Value,
                deliveryman.City.Name.Value,
                deliveryman.PhoneNumber))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeliverymanInfo>> GetAllDeliverymenInfoAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext
            .Deliverymen.AsNoTracking()
            .Select(d => new DeliverymanInfo(d.Id, d.Name.Value))
            .ToListAsync(cancellationToken);
    }
}
