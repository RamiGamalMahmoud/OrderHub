using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Setup.Categories;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Index;

internal sealed class GetProductIndexHandler(IProductReader reader)
    : IRequestHandler<GetProductIndexQuery, PagedResult<ProductIndexItem>>
{
    public Task<PagedResult<ProductIndexItem>> Handle(GetProductIndexQuery request, CancellationToken cancellationToken) =>
        reader.GetIndexAsync(request, cancellationToken);
}
