using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Application.Features.Setup.Categories;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class GetCategoryTreeQueryHandler(ICategoryReader reader) : IRequestHandler<GetCategoryTreeQuery, IEnumerable<CategoryTree>>
{
    public Task<IEnumerable<CategoryTree>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken) =>
        reader.GetCategoryTreeAsync(cancellationToken);
}
