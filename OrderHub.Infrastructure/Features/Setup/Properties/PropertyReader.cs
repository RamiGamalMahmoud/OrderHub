using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Setup.Properties;

internal sealed class PropertyReader(AppDbContextFactory dbContextFactory) : IPropertyReader
{
    public async Task<IReadOnlyList<PropertyListItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        using (AppDbContext dbContext = dbContextFactory.CreateDbContext())
        {
            return await dbContext.Properties
                .AsNoTracking()
                .Select(x => new PropertyListItem(x.Id, x.Name, x.Description, x.PropertyType))
                .ToListAsync(cancellationToken);
        }
    }

    public async Task<PropertyDetails> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using (AppDbContext dbContext = dbContextFactory.CreateDbContext())
        {
            return await dbContext.Properties
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new PropertyDetails(
                    x.Id,
                    x.Name,
                    x.PropertyType,
                    x.Description,
                    x.Options.Select(o => new PropertyOptionItem(
                        o.Id,
                        o.Value)).ToList()
                    ))
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
