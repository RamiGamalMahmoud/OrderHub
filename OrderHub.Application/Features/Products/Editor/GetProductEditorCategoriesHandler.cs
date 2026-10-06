using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Setup.Categories;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Editor;

internal sealed class GetProductEditorCategoriesHandler(IProductReader reader)
    : IRequestHandler<GetProductEditorCategoriesQuery, GetCategoriesResponse>
{
    public Task<GetCategoriesResponse> Handle(GetProductEditorCategoriesQuery request, CancellationToken cancellationToken) =>
        reader.BrowseEditorCategoriesAsync(request.ParentId, cancellationToken);
}
