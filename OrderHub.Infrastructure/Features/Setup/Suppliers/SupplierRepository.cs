using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.Suppliers;

internal sealed class SupplierRepository(AppDbContext context) : ISupplierRepository
{
    public Task<bool> IsUsedByOrderItemsAsync(int id, CancellationToken cancellationToken) =>
        context.OrderItems.AnyAsync(item => item.SupplierId == id, cancellationToken);

    public Task<bool> IsUsedByProductsAsync(int id, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(product => product.Suppliers.Any(s => s.Id == id), cancellationToken);

    public Task<Supplier> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        context
            .Suppliers
            .Include(s => s.Address).ThenInclude(a => a.City)
            .Include(s => s.Phone)
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

    public async Task<Supplier> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Suppliers.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Product>> GetProductsForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await context.Products
            .Include(product => product.Suppliers)
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetConnectedProductsForUpdateAsync(int supplierId, CancellationToken cancellationToken) =>
        await context.Products
            .Include(product => product.Suppliers)
            .Where(product => product.Suppliers.Any(supplier => supplier.Id == supplierId))
            .ToListAsync(cancellationToken);

    public void Add(Supplier entity) => context.Suppliers.Add(entity);

    public void Remove(Supplier entity) => context.Suppliers.Remove(entity);
}
