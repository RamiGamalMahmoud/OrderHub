using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

public record GetRootCategoriesQuery : IRequest<IEnumerable<CategoryInfo>>;
