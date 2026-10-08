using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class GetCategoriesInfoQueryHandler(ICategoryReader reader) : IRequestHandler<GetCategoriesInfoQuery, IEnumerable<CategoryInfo>>
{
    public Task<IEnumerable<CategoryInfo>> Handle(GetCategoriesInfoQuery request, CancellationToken cancellationToken) =>
        reader.GetCategoriesInfoAsync(cancellationToken);
}
