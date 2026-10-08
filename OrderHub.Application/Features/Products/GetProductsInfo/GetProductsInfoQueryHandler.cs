using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Products.Contracts;

namespace OrderHub.Application.Features.Products;

internal sealed class GetProductsInfoQueryHandler(IProductReader reader) : IRequestHandler<GetProductsInfoQuery, IEnumerable<ProductInfo>>
{
    public Task<IEnumerable<ProductInfo>> Handle(GetProductsInfoQuery request, CancellationToken cancellationToken) =>
        reader.GetProductsInfoAsync(cancellationToken);
}
