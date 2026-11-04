using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class GetSupplierEditorProductsHandler(ISupplierReader reader)
    : IRequestHandler<GetSupplierEditorProductsQuery, IEnumerable<ProductInfo>>
{
    public Task<IEnumerable<ProductInfo>> Handle(
        GetSupplierEditorProductsQuery request,
        CancellationToken cancellationToken) =>
        reader.GetSupplierEditorProductsAsync(cancellationToken);
}
