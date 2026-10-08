using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Suppliers.Contracts;

public interface ISupplierRepository
{
    Task<bool> IsUsedByOrderItemsAsync(int id, CancellationToken cancellationToken);
    Task<bool> IsUsedByProductsAsync(int id, CancellationToken cancellationToken);
    Task<Supplier> GetForUpdateAsync(int id, CancellationToken cancellationToken);
    Task<Supplier> FindAsync(int id, CancellationToken cancellationToken);
    void Add(Supplier entity);
    void Remove(Supplier entity);
}
