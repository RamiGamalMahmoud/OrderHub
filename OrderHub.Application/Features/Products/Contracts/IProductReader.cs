using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Editor;
using OrderHub.Application.Features.Products.Index;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.List;
using OrderHub.Application.Features.Setup.Categories;

namespace OrderHub.Application.Features.Products.Contracts;

public interface IProductReader
{
    Task<PagedResult<ProductIndexItem>> GetIndexAsync(
        GetProductIndexQuery query, CancellationToken cancellationToken);
    Task<ProductEditorData> GetEditorAsync(int? id, CancellationToken cancellationToken);
    Task<GetCategoriesResponse> BrowseEditorCategoriesAsync(int? parentId, CancellationToken cancellationToken);
    Task<IEnumerable<ProductListItem>> GetListProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDetails> GetProductAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLookupItem>> GetProductByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLookupItem>> GetProductsByName(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductInfo>> GetProductsInfoAsync(CancellationToken cancellationToken);
}
