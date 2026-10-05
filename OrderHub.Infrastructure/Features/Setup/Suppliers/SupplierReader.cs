using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.Setup.Suppliers;

internal sealed class SupplierReader(AppDbContextFactory contextFactory) : ISupplierReader
{
    public async Task<IEnumerable<SupplierListItem>> GetAllSuppliersAsync(CancellationToken cancellationToken)
    {
        using (AppDbContext appDbContext = contextFactory.CreateDbContext())
        {
            return await appDbContext
                .Suppliers.AsNoTracking()
                .Select(s => new SupplierListItem(
                    s.Id,
                    s.Name.Value,
                    s.BusinessHours.OpenAt, s.BusinessHours.CloseAt,
                    $"{s.Address.City.Name} - {s.Address.Street}", s.Phone.ToString()))
                .ToListAsync(cancellationToken: cancellationToken);
        }
    }

    public async Task<SupplierEdit> GetSupplierForEditAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        var result = await appDbContext
            .Suppliers.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(supplier => new SupplierEdit(
                supplier.Id,
                supplier.Name.Value,
                supplier.BusinessHours.OpenAt,
                supplier.BusinessHours.CloseAt,
                supplier.Address.Street,
                supplier.Address.City.Id,
                supplier.Phone.Number.NationalNumber,
                supplier.Phone.Number.CountryCode))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        // Preserve the existing missing-supplier exception rather than inventing a business failure.
        return result ?? throw new System.NullReferenceException();
    }

    public async Task<IEnumerable<SupplierInfo>> GetSuppliersInfoAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext
            .Suppliers.AsNoTracking()
            .Select(s => new SupplierInfo(s.Id, s.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
