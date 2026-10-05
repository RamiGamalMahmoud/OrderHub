using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class GetSubCategoriesQueryHandler(ICategoryReader reader) : IRequestHandler<GetSubCategoriesQuery, IEnumerable<CategoryInfo>>
{
    public Task<IEnumerable<CategoryInfo>> Handle(GetSubCategoriesQuery request, CancellationToken cancellationToken) =>
        reader.GetSubCategoriesAsync(request.ParentCategoryId, cancellationToken);
}
