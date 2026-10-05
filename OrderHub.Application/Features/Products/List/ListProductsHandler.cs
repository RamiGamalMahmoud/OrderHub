using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.List;

internal class ListProductsHandler : IRequestHandler<ListProductsQuery, IEnumerable<ProductListItem>>
{
    private readonly IProductReader _productStore;

    public ListProductsHandler(IProductReader productStore)
    {
        _productStore = productStore;
    }

    public async Task<IEnumerable<ProductListItem>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        return await _productStore.GetListProductsAsync(cancellationToken);
    }
}
