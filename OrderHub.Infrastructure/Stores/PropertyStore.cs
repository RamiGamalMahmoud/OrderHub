using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Stores;

internal class PropertyStore(AppDbContextFactory dbContextFactory) : IPropertyStore
{
    public async Task<int> CreateAsync(Property property, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = dbContextFactory.CreateDbContext();
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);
        return property.Id;
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = dbContextFactory.CreateDbContext();
        Property property = await dbContext.Properties.FindAsync(id);

        if (property is not null)
        {
            dbContext.Properties.Remove(property);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (System.Exception)
            {
                return Result.Failure();
            }
        }
        return Result.Failure();
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = dbContextFactory.CreateDbContext();

        return await dbContext.Properties.AnyAsync(x => x.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<PropertyListDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        using (AppDbContext dbContext = dbContextFactory.CreateDbContext())
        {
            return await dbContext.Properties
                .Select(x => new PropertyListDto(x.Id, x.Name, x.Description, x.PropertyType))
                .ToListAsync(cancellationToken);
        }
    }

    public async Task<PropertyDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using (AppDbContext dbContext = dbContextFactory.CreateDbContext())
        {
            return await dbContext.Properties
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new PropertyDetailsDto(
                    x.Id,
                    x.Name,
                    x.PropertyType,
                    x.Description,
                    x.Options.Select(o => new PropertyOptionDto(
                        o.Id,
                        o.Value)).ToList()
                    ))
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    public async Task UpdateAsync(PropertyUpdateDto dto, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = dbContextFactory.CreateDbContext();
        Property property = await dbContext.Properties
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken)
            ?? throw new Domain.Exceptions.DomainException("Property not found.");

        var options = await dbContext.PropertyOptions.Where(x => x.PropertyId == dto.Id).ToListAsync();

        property.Rename(dto.Name);
        property.ChangeType(dto.PropertyType);
        property.ChangeDescription(dto.Description);

        HashSet<int> optionIds = dto.Options
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        foreach (PropertyOption option in property.Options.ToList())
        {
            if (!optionIds.Contains(option.Id))
            {
                property.RemoveOption(option.Id);
            }
        }

        foreach (PropertyOptionUpdateDto optionUpdateDto in dto.Options)
        {
            if (!optionUpdateDto.Id.HasValue)
            {
                property.AddOption(optionUpdateDto.Value);
            }

            if (optionUpdateDto.Id.HasValue && property.Options.Any(x => x.Id == optionUpdateDto.Id.Value))
            {
                PropertyOption propertyOption = property.Options.First(x => x.Id == optionUpdateDto.Id.Value);
                if (propertyOption.Value != optionUpdateDto.Value)
                {
                    propertyOption.Rename(optionUpdateDto.Value);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

    }
}
