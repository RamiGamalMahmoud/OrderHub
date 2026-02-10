using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.Queries.CommonQueries;

namespace OrderHub.Infrastructure.QueryHandlers.CommonQueryHandlers;

internal class GetSuppliersInfoQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetSuppliersInfoQuery, IEnumerable<SupplierInfoDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<SupplierInfoDto>> Handle(GetSuppliersInfoQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        return await appDbContext
            .Suppliers
            .Select(s => new SupplierInfoDto(s.Id, s.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
