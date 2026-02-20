using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ProductDtos;
using static OrderHub.Application.Queries.ProductQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Products;

internal class GetProductsByNameQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetProductsByNameQuery, IEnumerable<ProductListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<ProductListDto>> Handle(GetProductsByNameQuery request, CancellationToken cancellationToken)
    {
        using (AppDbContext dbContext = _appDbContextFactory.CreateDbContext())
        {
            return await dbContext
                .Products
                .AsNoTracking()
                .Where(p => p.Name.Value.Contains(request.SearchTerm.Trim()))
                .Select(p => new ProductListDto(
                    p.Id,
                    p.Name.Value,
                    p.Price.Value,
                    p.Code,
                    p.Category.Name.Value,
                    p.Suppliers.Select(s => s.Id).ToImmutableList()))
                .ToListAsync(cancellationToken: cancellationToken);
        }
    }
}
