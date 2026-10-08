using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.Categories;

internal sealed class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task LockHierarchyAsync(CancellationToken cancellationToken)
    {
        // Hold until the operation's unit-of-work transaction finishes. Concurrent moves
        // must not both validate against an old hierarchy and then create a cycle.
        if (context.Database.IsNpgsql())
            await context.Database.ExecuteSqlRawAsync("LOCK TABLE categories IN SHARE ROW EXCLUSIVE MODE", cancellationToken);
    }

    public Task<bool> ExistsAsync(string name, int? parentId, CancellationToken cancellationToken) =>
        context.Categories
            .Where(c =>
                c.Name.Value == name &&
                c.ParentCategoryId == parentId)
            .AnyAsync(cancellationToken);

    public Task<bool> IsUsedByProductsAsync(int id, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(product => product.Category.Id == id, cancellationToken);

    public Task<bool> IsUsedByCategoriesAsync(int id, CancellationToken cancellationToken) =>
        context.Categories.AnyAsync(item => item.ParentCategoryId == id, cancellationToken);

    public async Task<Category> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Categories.FindAsync([id], cancellationToken);

    public async Task<bool> WouldCreateCycleAsync(int categoryId, int parentId, CancellationToken cancellationToken)
    {
        // UNION (rather than UNION ALL) also terminates on pre-existing corrupt cycles.
        var ancestors = await context.Database.SqlQuery<int>($"""
            WITH RECURSIVE ancestors AS (
                SELECT id, parent_category_id FROM categories WHERE id = {parentId}
                UNION
                SELECT c.id, c.parent_category_id FROM categories c
                JOIN ancestors a ON c.id = a.parent_category_id
            )
            SELECT id AS "Value" FROM ancestors
            """).ToListAsync(cancellationToken);
        return ancestors.Contains(categoryId);
    }

    public void Add(Category entity) => context.Categories.Add(entity);

    public void Remove(Category entity) => context.Categories.Remove(entity);
}
