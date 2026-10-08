using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Get;

internal class GetProductHandler : IRequestHandler<GetProductQuery, ProductDetails>
{
    private readonly IProductReader _productStore;

    public GetProductHandler(IProductReader productStore)
    {
        _productStore = productStore;
    }

    public async Task<ProductDetails> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        return await _productStore.GetProductAsync(request.Id, cancellationToken);
    }
}
