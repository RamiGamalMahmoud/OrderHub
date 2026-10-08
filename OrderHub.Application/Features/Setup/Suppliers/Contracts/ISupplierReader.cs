using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Suppliers;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Application.Features.Setup.Suppliers.Contracts;

public interface ISupplierReader
{
    Task<IEnumerable<SupplierListItem>> GetAllSuppliersAsync(CancellationToken cancellationToken);
    Task<SupplierEdit> GetSupplierForEditAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<SupplierInfo>> GetSuppliersInfoAsync(CancellationToken cancellationToken);
}
