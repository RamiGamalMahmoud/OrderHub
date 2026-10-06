using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Application.Features.Setup.Categories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.Setup.Categories;

internal sealed class CategoryReader(AppDbContextFactory contextFactory) : ICategoryReader
{
    private async Task<CategoryHierarchy> LoadHierarchyAsync(CancellationToken cancellationToken, bool includeProductCounts = true)
    {
        await using var context = contextFactory.CreateDbContext();
        var rows = await context.Categories.AsNoTracking()
            .Select(c => new CategoryHierarchyRow(c.Id, c.Name.Value, c.ParentCategoryId, includeProductCounts ? c.Products.Count() : 0))
            .ToListAsync(cancellationToken);
        return new CategoryHierarchy(rows);
    }

    public async Task<GetCategoriesResponse> GetCategoriesAsync(int? parentId, CancellationToken cancellationToken) =>
        (await LoadHierarchyAsync(cancellationToken)).Browse(parentId);

    public async Task<CategoryEdit> GetCategoryForEditAsync(int id, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();
        return await context.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryEdit(c.Id, c.Name.Value, c.ParentCategoryId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<CategoryInfo>> GetCategoriesInfoAsync(CancellationToken cancellationToken) =>
        (await LoadHierarchyAsync(cancellationToken, includeProductCounts: false)).Options();
}
