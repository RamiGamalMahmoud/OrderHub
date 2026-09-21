using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;
public interface ISupplierRepository
{
    void Add(Supplier supplier);
    Task<Supplier> GetById(int supplierId);
}
