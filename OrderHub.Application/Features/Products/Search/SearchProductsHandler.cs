using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Search;

internal sealed class SearchProductsHandler(IProductReader reader)
    : IRequestHandler<SearchProductsQuery, PagedResult<ProductLookupItem>>
{
    public Task<PagedResult<ProductLookupItem>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken) =>
        reader.GetProductsByName(
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);
}
