using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

[RequirePermission(PermissionFeature.Categories, PermissionLevel.Read)]
public record GetRootCategoriesQuery : IRequest<IEnumerable<CategoryInfo>>;
