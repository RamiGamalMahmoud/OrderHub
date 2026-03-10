using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Queries;
using OrderHub.Infrastructure.Extensions;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.Infrastructure.QueryHandlers.Products;

internal class GetAllProductsPagedQueryHandler(AppDbContextFactory appDbContextFactory) 
    : IRequestHandler<ProductQueries.GetAllProductsPagedQuery, PagedResult<ProductListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<PagedResult<ProductListDto>> Handle(ProductQueries.GetAllProductsPagedQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = _appDbContextFactory.CreateDbContext();

        var query = dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name.Value)
            .Select(p => new ProductListDto(
                p.Id,
                p.Name.Value,
                p.Price.Value,
                p.Code,
                p.Category.Name.Value,
                p.Suppliers.Select(s => s.Id).ToImmutableList(),
                p.Suppliers.Select(s => new Application.DTOs.CommonDtos.SupplierInfoDto(s.Id, s.Name.Value)).ToImmutableList()
                ));

        return await query.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
