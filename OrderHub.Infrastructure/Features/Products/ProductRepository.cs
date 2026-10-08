using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Products;

internal sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<Category> FindCategoryAsync(int id, CancellationToken cancellationToken) =>
        await context.Categories.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync(IEnumerable<int> ids, CancellationToken cancellationToken) =>
        await context.Suppliers.Where(supplier => ids.Contains(supplier.Id)).ToListAsync(cancellationToken);

    public Task<Product> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        context.Products.Include(product => product.Category).Include(product => product.Suppliers)
            .Include(product => product.Properties).FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<Product> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Products.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<int>> GetExistingPropertyIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken) =>
        await context.Properties.AsNoTracking().Where(property => ids.Contains(property.Id))
            .Select(property => property.Id).ToListAsync(cancellationToken);

    public async Task<bool> HasBusinessReferencesAsync(int id, CancellationToken cancellationToken)
    {
        if (await context.OrderItems.AnyAsync(item => item.ProductId == id, cancellationToken) ||
            await context.InvoicesItem.AnyAsync(item => item.ProductId == id, cancellationToken) ||
            await context.QuotationItems.AnyAsync(item => item.ProductId == id, cancellationToken) ||
            await context.ProformaInvoiceItems.AnyAsync(item => item.ProductId == id, cancellationToken))
        {
            return true;
        }

        if (context.Database.IsNpgsql())
        {
            string productId = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return await context.DraftRecords.FromSqlInterpolated($$"""
                SELECT * FROM order_drafts
                WHERE EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(
                        CASE WHEN jsonb_typeof(data #> '{Data,Items}') = 'array'
                            THEN data #> '{Data,Items}' ELSE '[]'::jsonb END
                    ) item
                    WHERE item->>'ProductId' = {{productId}}
                )
                """)
                .AsNoTracking()
                .AnyAsync(cancellationToken);
        }

        return await context.DraftRecords.FromSqlInterpolated($"""
            SELECT * FROM order_drafts
            WHERE json_valid(data) AND EXISTS (
                SELECT 1 FROM json_each(data, '$.Data.Items') item
                WHERE json_extract(item.value, '$.ProductId') = {id}
            )
            """)
            .AsNoTracking()
            .AnyAsync(cancellationToken);
    }

    public void Add(Product product) => context.Products.Add(product);
    public void Remove(Product product) => context.Products.Remove(product);
}
