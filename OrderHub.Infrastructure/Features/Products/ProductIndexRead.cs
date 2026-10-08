using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Index;
using OrderHub.Infrastructure.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Products;

internal sealed partial class ProductReader
{
    public async Task<PagedResult<ProductIndexItem>> GetIndexAsync(GetProductIndexQuery request, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.CreateDbContext();
        var query = context.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(product => product.Name.Value.ToLower().Contains(term) ||
                product.Code.ToLower().Contains(term) || product.Category.Name.Value.ToLower().Contains(term) ||
                product.Suppliers.Any(supplier => supplier.Name.Value.ToLower().Contains(term)));
        }
        return await query.OrderBy(product => product.Name.Value).ThenBy(product => product.Id)
            .Select(product => new ProductIndexItem(product.Id, product.Name.Value, product.Code, product.Price.Value,
                product.Category.Name.Value, product.Suppliers.Count, product.Properties.Count))
            .ToPagedResultAsync(System.Math.Max(1, request.Page), System.Math.Clamp(request.PageSize, 1, 50), cancellationToken);
    }
}
