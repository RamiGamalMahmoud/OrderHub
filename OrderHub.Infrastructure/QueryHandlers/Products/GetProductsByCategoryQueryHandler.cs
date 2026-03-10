using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ProductDtos;
using static OrderHub.Application.Queries.ProductQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Products;

internal class GetProductsByCategoryQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetProductsByCategoryQuery, IEnumerable<ProductListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<ProductListDto>> Handle(GetProductsByCategoryQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        string sql = """
            WITH RECURSIVE category_tree AS (
                -- Anchor member: start with root category
                SELECT id, name, parent_category_id, 0 as level
                FROM categories
                WHERE id = @p0 

                UNION ALL

                -- Recursive member: get children
                SELECT c.id, c.name, c.parent_category_id, ct.level + 1
                FROM categories c
                JOIN category_tree ct ON c.parent_category_id = ct.id
            )
            SELECT * FROM category_tree
            ORDER BY level, name
            """;

        IEnumerable<int> categories = await appDbContext
            .Categories
            .FromSqlRaw(sql, request.CategoryId)
            .Select(c => c.Id)
            .ToListAsync();

        return await appDbContext.Products
            .Where(p => categories.Contains(p.Category.Id))
            .Select(p => new ProductListDto(
                p.Id, 
                p.Name.Value, 
                p.Price.Value, 
                p.Code, 
                p.Category.Name.Value, 
                p.Suppliers.Select(s => s.Id)
                    .ToImmutableList(),
                p.Suppliers.Select(s => new Application.DTOs.CommonDtos.SupplierInfoDto(s.Id, s.Name.Value)).ToImmutableList()
                ))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
