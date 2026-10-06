using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Setup.Properties;

internal sealed class PropertyReader(AppDbContextFactory contextFactory) : IPropertyReader
{
    public async Task<IReadOnlyList<PropertyListItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using AppDbContext context = contextFactory.CreateDbContext();

        return await context.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .ThenBy(property => property.Id)
            .Select(property => new PropertyListItem(
                property.Id,
                property.Name,
                property.Description,
                property.PropertyType,
                property.Options.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyDetails> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using AppDbContext context = contextFactory.CreateDbContext();

        return await context.Properties
            .AsNoTracking()
            .Where(property => property.Id == id)
            .Select(property => new PropertyDetails(
                property.Id,
                property.Name,
                property.PropertyType,
                property.Description,
                property.Options.OrderBy(option => option.Id)
                    .Select(option => new PropertyOptionItem(option.Id, option.Value)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
