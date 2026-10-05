using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Application.Features.Setup.Categories;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class GetCategoryListQueryHandler(ICategoryReader reader) : IRequestHandler<GetCategoryListQuery, IEnumerable<CategoryListItem>>
{
    public Task<IEnumerable<CategoryListItem>> Handle(GetCategoryListQuery request, CancellationToken cancellationToken) =>
        reader.GetCategoryListAsync(request.ParentId, cancellationToken);
}
