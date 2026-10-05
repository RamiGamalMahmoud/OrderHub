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
    public async Task<CategoryEdit> GetCategoryForEditAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext
            .Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryEdit(c.Id, c.Name.Value, c.ParentCategoryId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<CategoryListItem>> GetCategoryListAsync(int? parentId, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();

        IEnumerable<CategoryListItem> categories = await appDbContext
            .Categories.AsNoTracking()
            .Where(c => c.ParentCategoryId == parentId)
            .Select(c => new CategoryListItem(
                c.Id,
                c.Name.Value,
                c.SubCategories.Count > 0,
                c.SubCategories.Count,
                c.Products.Count(),
                c.ParentCategoryId))

            .ToListAsync(cancellationToken);

        return categories;
    }

    public async Task<IEnumerable<int>> GetCategoryParentIdsAsync(int categoryId, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        string sql = $@"WITH RECURSIVE ParentHierarchy AS (
                    -- Start with the child
                    SELECT id, name, parent_category_id
                    FROM categories
                    WHERE Id = @child_id

                    UNION ALL

                    -- Get parent
                    SELECT c.id, c.name, c.parent_category_id
                    FROM categories c
                    INNER JOIN ParentHierarchy ph ON c.id = ph.parent_category_id
                    )
                    SELECT id
                    FROM ParentHierarchy";
        return await appDbContext.Database.SqlQueryRaw<int>(sql, new Npgsql.NpgsqlParameter("@child_id", categoryId)).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CategoryTree>> GetCategoryTreeAsync(CancellationToken cancellationToken)
    {
        await using var appDbContext = contextFactory.CreateDbContext();

        IEnumerable<CategoryFlatItem> allCategories = await appDbContext
            .Categories.AsNoTracking()
            .Select(c => new CategoryFlatItem(
                c.Id,
                c.Name.Value,
                c.SubCategories.Count != 0,
                c.ParentCategoryId))
            .ToListAsync(cancellationToken);

        return BuildCategoryTree(allCategories);
    }

    private static List<CategoryTree> BuildCategoryTree(
            IEnumerable<CategoryFlatItem> flatCategories)
    {
        ILookup<int?, CategoryFlatItem> lookup =
            flatCategories.ToLookup(c => c.ParentCategoryId);

        List<CategoryTree> BuildTree(int? parentId)
        {
            return lookup[parentId]
                .Select(child => new CategoryTree(
                    child.Id,
                    child.Name,
                    child.IsRootCategory,
                    BuildTree(child.Id)))
                .ToList();
        }

        return BuildTree(null);
    }

    private record CategoryFlatItem(
        int Id,
        string Name,
        bool IsRootCategory,
        int? ParentCategoryId);

    public async Task<IEnumerable<CategoryInfo>> GetCategoriesInfoAsync(CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = contextFactory.CreateDbContext();

        // Legacy info reads used fresh, untracked categories without ancestors loaded.
        // Their FullPath was the category's own name; retain that result without materializing entities.
        return await dbContext
            .Categories.AsNoTracking()
            .Select(c => new CategoryInfo(c.Id, c.Name.Value, c.Name.Value, c.SubCategories.Count < 0, c.ParentCategoryId))
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<CategoryInfo>> GetRootCategoriesAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext.Categories.AsNoTracking()
            .Where(c => c.ParentCategory == null)
            .Select(c => new CategoryInfo(c.Id, c.Name.Value, c.Name.Value, c.SubCategories.Count < 0, c.ParentCategoryId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CategoryInfo>> GetSubCategoriesAsync(int parentCategoryId, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext.Categories.AsNoTracking()
            .Where(c => c.ParentCategoryId == parentCategoryId)
            .Select(c => new CategoryInfo(c.Id, c.Name.Value, c.Name.Value, c.SubCategories.Count < 0, c.ParentCategoryId))
            .ToListAsync(cancellationToken);
    }
}
