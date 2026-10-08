using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
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

    public void Add(Supplier entity) => context.Suppliers.Add(entity);

    public void Remove(Supplier entity) => context.Suppliers.Remove(entity);
}
