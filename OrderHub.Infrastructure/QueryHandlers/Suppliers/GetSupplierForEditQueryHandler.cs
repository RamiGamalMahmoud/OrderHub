using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.SupplierDtos;
using static OrderHub.Application.Queries.SupplierQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Suppliers;

internal class GetSupplierForEditQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetSupplierForEditQuery, SupplierEditDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<SupplierEditDto> Handle(GetSupplierForEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        Supplier supplier = await appDbContext
            .Suppliers
            .AsNoTracking()
            .Include(s => s.Address).ThenInclude(a => a.City)
            .Include(s => s.Phone)
            .Where(s => s.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return new SupplierEditDto(
            supplier.Id,
            supplier.Name.Value,
            supplier.BusinessHours.OpenAt,
            supplier.BusinessHours.CloseAt,
            supplier.Address.Street,
            supplier.Address.City.Id,
            supplier.Phone.Number.NationalNumber,
            supplier.Phone.Number.CountryCode
            );
    }
}
