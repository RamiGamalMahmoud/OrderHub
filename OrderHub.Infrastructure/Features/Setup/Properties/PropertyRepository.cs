using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Setup.Properties;

internal sealed class PropertyRepository(AppDbContext context) : IPropertyRepository
{
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        context.Properties.AnyAsync(property => property.Name == name, cancellationToken);
    public Task<Property> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        context.Properties.Include(property => property.Options).FirstOrDefaultAsync(property => property.Id == id, cancellationToken);
    public async Task<Property> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Properties.FindAsync([id], cancellationToken);
    public void Add(Property property) => context.Properties.Add(property);
    public void Remove(Property property) => context.Properties.Remove(property);
}
