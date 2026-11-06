using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Categories.GetCategoryColors;

[RequirePermission(PermissionFeature.Categories, PermissionLevel.Read)]
public sealed record GetCategoryColorsQuery : IRequest<IReadOnlyList<string>>;
