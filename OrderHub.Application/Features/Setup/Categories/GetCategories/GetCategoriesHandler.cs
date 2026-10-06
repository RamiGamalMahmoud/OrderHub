using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class GetCategoriesHandler(ICategoryReader reader) : IRequestHandler<GetCategoriesQuery, GetCategoriesResponse>
{
    public Task<GetCategoriesResponse> Handle(GetCategoriesQuery request, CancellationToken cancellationToken) =>
        reader.GetCategoriesAsync(request.ParentId, cancellationToken);
}
