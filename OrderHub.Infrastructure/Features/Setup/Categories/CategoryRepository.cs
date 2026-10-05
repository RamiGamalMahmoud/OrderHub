using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.Categories;

internal sealed class CategoryRepository(AppDbContext context) : ICategoryRepository
{
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

    public void Add(Category entity) => context.Categories.Add(entity);

    public void Remove(Category entity) => context.Categories.Remove(entity);
}
