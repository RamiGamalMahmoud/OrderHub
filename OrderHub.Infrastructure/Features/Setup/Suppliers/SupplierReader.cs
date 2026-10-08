using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Setup.Suppliers;

internal sealed class SupplierReader(AppDbContextFactory contextFactory) : ISupplierReader
{
    public async Task<IEnumerable<SupplierListItem>> GetAllSuppliersAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();

        return await context.Suppliers.AsNoTracking()
            .OrderBy(supplier => supplier.Name.Value)
            .ThenBy(supplier => supplier.Id)
            .Select(supplier => new SupplierListItem(
                supplier.Id,
                supplier.Name.Value,
                supplier.BusinessHours != null ? supplier.BusinessHours.OpenAt : default,
                supplier.BusinessHours != null ? supplier.BusinessHours.CloseAt : default,
                supplier.Address != null ? supplier.Address.City.Name.Value + " - " + supplier.Address.Street : "",
                supplier.Phone != null ? supplier.Phone.Number.CountryCode + " - " + supplier.Phone.Number.NationalNumber : ""))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierEdit> GetSupplierForEditAsync(int id, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();

        return await context.Suppliers.AsNoTracking()
            .Where(supplier => supplier.Id == id)
            .Select(supplier => new SupplierEdit(
                supplier.Id,
                supplier.Name.Value,
                supplier.BusinessHours != null ? supplier.BusinessHours.OpenAt : (TimeOnly?)null,
                supplier.BusinessHours != null ? supplier.BusinessHours.CloseAt : (TimeOnly?)null,
                supplier.Address != null ? supplier.Address.Street : "",
                supplier.Address != null ? supplier.Address.City.Id : 0,
                supplier.Phone != null ? supplier.Phone.Number.NationalNumber : "",
                supplier.Phone != null ? supplier.Phone.Number.CountryCode : ""))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<SupplierInfo>> GetSuppliersInfoAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();

        return await context.Suppliers.AsNoTracking()
            .Select(supplier => new SupplierInfo(supplier.Id, supplier.Name.Value))
            .ToListAsync(cancellationToken);
    }
}
