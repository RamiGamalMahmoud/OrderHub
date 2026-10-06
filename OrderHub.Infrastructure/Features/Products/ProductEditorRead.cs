using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Products.Editor;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Infrastructure.Features.Setup.Categories;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Products;

internal sealed partial class ProductReader
{
    private async Task<CategoryHierarchy> EditorHierarchyAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();
        var rows = await context.Categories.AsNoTracking()
            .Select(category => new CategoryHierarchyRow(category.Id, category.Name.Value, category.ParentCategoryId, 0))
            .ToListAsync(cancellationToken);
        return new CategoryHierarchy(rows);
    }

    public async Task<GetCategoriesResponse> BrowseEditorCategoriesAsync(int? parentId, CancellationToken cancellationToken) =>
        (await EditorHierarchyAsync(cancellationToken)).Browse(parentId);

    public async Task<ProductEditorData> GetEditorAsync(int? id, CancellationToken cancellationToken)
    {
        var product = id.HasValue ? await GetProductAsync(id.Value, cancellationToken) : null;
        if (id.HasValue && product is null)
        {
            return null;
        }
        await using var context = contextFactory.CreateDbContext();
        var suppliers = await context.Suppliers.AsNoTracking()
            .OrderBy(supplier => supplier.Name.Value).ThenBy(supplier => supplier.Id)
            .Select(supplier => new ProductEditorSupplier(supplier.Id, supplier.Name.Value))
            .ToListAsync(cancellationToken);
        var properties = await context.Properties.AsNoTracking()
            .OrderBy(property => property.Name).ThenBy(property => property.Id)
            .Select(property => new ProductEditorProperty(property.Id, property.Name, property.PropertyType, property.Description,
                property.Options.OrderBy(option => option.Value).ThenBy(option => option.Id)
                    .Select(option => new ProductEditorOption(option.Id, option.Value)).ToList()))
            .ToListAsync(cancellationToken);
        var hierarchy = await EditorHierarchyAsync(cancellationToken);
        return new ProductEditorData(product, suppliers, properties,
            product is null ? [] : hierarchy.Path(product.CategoryId));
    }
}
