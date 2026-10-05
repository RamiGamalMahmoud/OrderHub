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

    public Task<bool> IsUsedByOrdersAsync(int id, CancellationToken cancellationToken) =>
        context.OrderItems.AnyAsync(item => item.ProductId == id, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);
    public void Remove(Product product) => context.Products.Remove(product);
}
