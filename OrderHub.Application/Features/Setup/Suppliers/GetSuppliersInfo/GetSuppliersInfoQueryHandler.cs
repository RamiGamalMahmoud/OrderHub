using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class GetSuppliersInfoQueryHandler(ISupplierReader reader) : IRequestHandler<GetSuppliersInfoQuery, IEnumerable<SupplierInfo>>
{
    public Task<IEnumerable<SupplierInfo>> Handle(GetSuppliersInfoQuery request, CancellationToken cancellationToken) =>
        reader.GetSuppliersInfoAsync(cancellationToken);
}
