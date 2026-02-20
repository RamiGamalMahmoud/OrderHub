using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;
using static OrderHub.Application.Queries.CategoryQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Categories;

internal class GetCategoryListQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetCategoryListQuery, IEnumerable<CategoryListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<CategoryListDto>> Handle(GetCategoryListQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        IEnumerable<CategoryListDto> categories = await appDbContext
            .Categories
            .Where(c => c.ParentCategoryId == request.ParentId)
            .Select(c => new CategoryListDto(
                c.Id,
                c.Name.Value,
                c.SubCategories.Count > 0,
                c.SubCategories.Count,
                c.Products.Count(),
                c.ParentCategoryId))
            
            .ToListAsync(cancellationToken);

        return categories;
    }
}
