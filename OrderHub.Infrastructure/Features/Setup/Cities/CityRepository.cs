using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.Cities;

internal sealed class CityRepository(AppDbContext context) : ICityRepository
{
    public Task<bool> NameExistsAsync(
        string name,
        int? excludingId,
        CancellationToken cancellationToken) =>
        context.Cities.AnyAsync(
            city => city.Name.Value == name &&
                (!excludingId.HasValue || city.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> IsUsedByAddressesAsync(int id, CancellationToken cancellationToken) =>
        context.Addresses.AnyAsync(
            address => address.City.Id == id,
            cancellationToken);

    public Task<bool> IsUsedByDeliverymenAsync(int id, CancellationToken cancellationToken) =>
        context.Deliverymen.AnyAsync(
            deliveryman => deliveryman.City.Id == id,
            cancellationToken);

    public async Task<City> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Cities.FindAsync([id], cancellationToken);

    public void Add(City entity) => context.Cities.Add(entity);

    public void Remove(City entity) => context.Cities.Remove(entity);
}
