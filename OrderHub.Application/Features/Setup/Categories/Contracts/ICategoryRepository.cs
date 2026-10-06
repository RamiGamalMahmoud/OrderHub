using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(string name, int? parentId, CancellationToken cancellationToken);
    Task<bool> IsUsedByProductsAsync(int id, CancellationToken cancellationToken);
    Task<bool> IsUsedByCategoriesAsync(int id, CancellationToken cancellationToken);
    Task<Category> FindAsync(int id, CancellationToken cancellationToken);
    Task<bool> WouldCreateCycleAsync(int categoryId, int parentId, CancellationToken cancellationToken);
    Task LockHierarchyAsync(CancellationToken cancellationToken);
    void Add(Category entity);
    void Remove(Category entity);
}
