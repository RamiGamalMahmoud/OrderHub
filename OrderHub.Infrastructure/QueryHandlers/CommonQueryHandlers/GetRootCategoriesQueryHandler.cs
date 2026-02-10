using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;
using static OrderHub.Application.Queries.CommonQueries;

namespace OrderHub.Infrastructure.QueryHandlers.CommonQueryHandlers;

internal class GetRootCategoriesQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetRootCategoriesQuery, IEnumerable<CategoryInfoDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<CategoryInfoDto>> Handle(GetRootCategoriesQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        return await appDbContext.Categories
            .Where(c => c.ParentCategory == null)
            .Select(c => new CategoryInfoDto(c.Id, c.Name.Value, c.FullPath, c.SubCategories.Count < 0, c.ParentCategoryId))
            .ToListAsync();
    }
}
