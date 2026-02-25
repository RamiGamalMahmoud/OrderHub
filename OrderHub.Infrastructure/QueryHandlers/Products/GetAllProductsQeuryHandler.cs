using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Queries;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.Infrastructure.QueryHandlers.Products
{
    internal class GetAllProductsQeuryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<ProductQueries.GetAllProductsQuery, IEnumerable<ProductListDto>>
    {
        public async Task<IEnumerable<ProductListDto>> Handle(ProductQueries.GetAllProductsQuery request, CancellationToken cancellationToken)
        {
            using (AppDbContext dbContext = appDbContextFactory.CreateDbContext())
            {
                int productsCount = await dbContext.Products.CountAsync();
                return await dbContext
                    .Products
                    .AsNoTracking()
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
}
