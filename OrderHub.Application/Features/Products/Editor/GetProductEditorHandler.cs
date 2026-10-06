using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Setup.Categories;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Editor;

internal sealed class GetProductEditorHandler(IProductReader reader)
    : IRequestHandler<GetProductEditorQuery, ProductEditorData>
{
    public Task<ProductEditorData> Handle(GetProductEditorQuery request, CancellationToken cancellationToken) =>
        reader.GetEditorAsync(request.ProductId, cancellationToken);
}
