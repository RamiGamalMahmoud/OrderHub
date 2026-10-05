using MediatR;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Suppliers;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class GetSupplierForEditQueryHandler(ISupplierReader reader) : IRequestHandler<GetSupplierForEditQuery, SupplierEdit>
{
    public Task<SupplierEdit> Handle(GetSupplierForEditQuery request, CancellationToken cancellationToken) =>
        reader.GetSupplierForEditAsync(request.Id, cancellationToken);
}
