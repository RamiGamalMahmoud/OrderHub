using System.Collections.Generic;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.List;
using OrderHub.Application.Features.Products.Update;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Contracts;

public interface IProductReader
{
    Task<IEnumerable<ProductListItem>> GetListProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDetails> GetProductAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLookupItem>> GetProductByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLookupItem>> GetProductsByName(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductInfo>> GetProductsInfoAsync(CancellationToken cancellationToken);
}
