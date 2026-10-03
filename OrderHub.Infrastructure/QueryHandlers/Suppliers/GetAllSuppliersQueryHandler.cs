using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.SupplierDtos;
using static OrderHub.Application.Queries.SupplierQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Suppliers;

internal class GetAllSuppliersQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetAllSuppliersQuery, IEnumerable<SupplierListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<SupplierListDto>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
    {
        using (AppDbContext appDbContext = _appDbContextFactory.CreateDbContext())
        {
            return await appDbContext
                .Suppliers
                .AsNoTracking()
                .Select(s => new SupplierListDto(
                    s.Id,
                    s.Name.Value,
                    s.BusinessHours.OpenAt, s.BusinessHours.CloseAt,
                    $"{s.Address.City.Name} - {s.Address.Street}", s.Phone.ToString()))
                .ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
