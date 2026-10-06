using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Setup.Properties;

internal sealed class PropertyRepository(AppDbContext context) : IPropertyRepository
{
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        context.Properties.AnyAsync(property => property.Name == name, cancellationToken);

    public Task<bool> NameExistsForOtherAsync(string name, int id, CancellationToken cancellationToken) =>
        context.Properties.AnyAsync(
            property => property.Name == name && property.Id != id,
            cancellationToken);

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Set<ProductProperty>().AnyAsync(item => item.PropertyId == id, cancellationToken) ||
            await context.OrderItemProperties.AnyAsync(item => item.PropertyId == id, cancellationToken);
    }

    public Task<bool> AreOptionValuesInUseAsync(
        int propertyId,
        IReadOnlyCollection<string> values,
        CancellationToken cancellationToken) =>
        context.OrderItemProperties.AnyAsync(
            item => item.PropertyId == propertyId && values.Contains(item.Value),
            cancellationToken);

    public async Task<Property> GetForUpdateAsync(int id, CancellationToken cancellationToken)
    {
        if (context.Database.IsNpgsql())
        {
            // The application's update transaction holds this row lock until commit.
            // Read options afterward so concurrent editors validate the latest definition.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM properties WHERE id = {id} FOR UPDATE",
                cancellationToken);
        }

        return await context.Properties
            .Include(property => property.Options)
            .FirstOrDefaultAsync(property => property.Id == id, cancellationToken);
    }

    public async Task<Property> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Properties.FindAsync([id], cancellationToken);

    public async Task PrepareOptionRenamesAsync(int propertyId, CancellationToken cancellationToken)
    {
        context.ChangeTracker.DetectChanges();

        var renamed = context.ChangeTracker.Entries<PropertyOption>()
            .Where(entry => entry.Entity.PropertyId == propertyId &&
                entry.State == EntityState.Modified && entry.Property(option => option.Value).IsModified)
            .ToList();

        if (renamed.Count == 0)
        {
            return;
        }

        // Vacate old unique values in one bulk write, allowing swaps without recreating
        // options. Temporary values remain private to the existing update transaction.
        string prefix = "__oh_" + Guid.NewGuid().ToString("N") + "_";
        string ids = string.Join(",", renamed.Select(entry => entry.Entity.Id.ToString(CultureInfo.InvariantCulture)));

        await context.Database.ExecuteSqlRawAsync(
            "UPDATE property_options SET value = {0} || CAST(id AS TEXT) WHERE id IN (" + ids + ")",
            new object[] { prefix },
            cancellationToken);

        foreach (var entry in renamed)
        {
            entry.Property(option => option.Value).OriginalValue =
                prefix + entry.Entity.Id.ToString(CultureInfo.InvariantCulture);
        }
    }

    public void Add(Property property) => context.Properties.Add(property);

    public void Remove(Property property) => context.Properties.Remove(property);
}
