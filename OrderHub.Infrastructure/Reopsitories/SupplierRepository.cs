using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;
internal class SupplierRepository(AppDbContext appDbContext) :
    RepositoryBase<Supplier>(appDbContext), ISupplierRepository
{
    public override void Add(Supplier model)
    {
        _appDbContext.Suppliers.Add(model);
    }

    public async Task<Supplier> GetById(int supplierId)
    {
        return await _appDbContext
            .Suppliers
            .SingleOrDefaultAsync(supplier => supplier.Id == supplierId);
    }
}
