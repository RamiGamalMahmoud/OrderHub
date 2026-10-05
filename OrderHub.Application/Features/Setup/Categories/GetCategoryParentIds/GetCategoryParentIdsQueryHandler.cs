using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class GetCategoryParentIdsQueryHandler(ICategoryReader reader) : IRequestHandler<GetCategoryParentIdsQuery, IEnumerable<int>>
{
    public Task<IEnumerable<int>> Handle(GetCategoryParentIdsQuery request, CancellationToken cancellationToken) =>
        reader.GetCategoryParentIdsAsync(request.CategoryId, cancellationToken);
}
