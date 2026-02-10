using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.Queries.CommonQueries;

namespace OrderHub.Infrastructure.QueryHandlers.CommonQueryHandlers;

internal class GetProductsInfoQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetProductsIfoQuery, IEnumerable<ProductInfoDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<ProductInfoDto>> Handle(GetProductsIfoQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        return await appDbContext
            .Products
            .Select(p => new ProductInfoDto(p.Id, p.Name.Value))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
