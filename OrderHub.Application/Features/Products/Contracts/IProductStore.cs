using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.List;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Contracts;

public interface IProductStore
{
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> CreateAsync(CreateProduct.ProductDto product);
    Task<Result<int>> UpdateAsync(int productId, UpdateProduct.ProductDto product, CancellationToken cancellationToken = default);
    Task<IEnumerable<ListProducts.ProductDto>> GetListProductsAsync(CancellationToken cancellationToken = default);
    Task<GetProduct.ProductDetails> GetProductAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLookupItem>> GetProductByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLookupItem>> GetProductsByName(string name, CancellationToken cancellationToken = default);
}
