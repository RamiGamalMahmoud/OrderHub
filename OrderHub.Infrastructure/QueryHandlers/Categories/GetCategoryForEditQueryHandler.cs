using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;
using static OrderHub.Application.Queries.CategoryQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Categories;

internal class GetCategoryForEditQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetCategoryForEditQuery, CategoryEditDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<CategoryEditDto> Handle(GetCategoryForEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        return await appDbContext
            .Categories
            .Where(c => c.Id == request.Id)
            .Select(c => new CategoryEditDto(c.Id, c.Name.Value, c.ParentCategoryId))
            .FirstOrDefaultAsync();
    }
}
