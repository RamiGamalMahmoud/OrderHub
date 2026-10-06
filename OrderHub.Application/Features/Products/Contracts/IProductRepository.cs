using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Contracts;

public interface IProductRepository
{
    Task<Category> FindCategoryAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync(IEnumerable<int> ids, CancellationToken cancellationToken);
    Task<Product> GetForUpdateAsync(int id, CancellationToken cancellationToken);
    Task<Product> FindAsync(int id, CancellationToken cancellationToken);
    Task<bool> HasBusinessReferencesAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<int>> GetExistingPropertyIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken);
    void Add(Product product);
    void Remove(Product product);
}
