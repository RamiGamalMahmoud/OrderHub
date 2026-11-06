using MediatR;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Categories.GetCategoryColors;

internal sealed class GetCategoryColorsQueryHandler(ICategoryReader reader)
    : IRequestHandler<GetCategoryColorsQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(
        GetCategoryColorsQuery request,
        CancellationToken cancellationToken) =>
        reader.GetCategoryColorsAsync(cancellationToken);
}
