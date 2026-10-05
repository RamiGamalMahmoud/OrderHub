using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Suppliers;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class GetAllSuppliersQueryHandler(ISupplierReader reader) : IRequestHandler<GetAllSuppliersQuery, IEnumerable<SupplierListItem>>
{
    public Task<IEnumerable<SupplierListItem>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken) =>
        reader.GetAllSuppliersAsync(cancellationToken);
}
