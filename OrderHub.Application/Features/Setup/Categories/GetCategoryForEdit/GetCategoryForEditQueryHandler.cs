using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Application.Features.Setup.Categories;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class GetCategoryForEditQueryHandler(ICategoryReader reader) : IRequestHandler<GetCategoryForEditQuery, CategoryEdit>
{
    public Task<CategoryEdit> Handle(GetCategoryForEditQuery request, CancellationToken cancellationToken) =>
        reader.GetCategoryForEditAsync(request.Id, cancellationToken);
}
