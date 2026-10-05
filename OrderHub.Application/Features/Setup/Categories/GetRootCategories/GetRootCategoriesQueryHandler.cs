using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class GetRootCategoriesQueryHandler(ICategoryReader reader) : IRequestHandler<GetRootCategoriesQuery, IEnumerable<CategoryInfo>>
{
    public Task<IEnumerable<CategoryInfo>> Handle(GetRootCategoriesQuery request, CancellationToken cancellationToken) =>
        reader.GetRootCategoriesAsync(cancellationToken);
}
