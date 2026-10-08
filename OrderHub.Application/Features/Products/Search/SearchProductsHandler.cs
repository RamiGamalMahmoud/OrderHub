using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Search;

internal sealed class SearchProductsHandler(IProductReader reader) : IRequestHandler<SearchProductsQuery, IEnumerable<ProductLookupItem>>
{
    public Task<IEnumerable<ProductLookupItem>> Handle(SearchProductsQuery request, CancellationToken cancellationToken) =>
        reader.GetProductsByName(request.SearchTerm, cancellationToken);
}
