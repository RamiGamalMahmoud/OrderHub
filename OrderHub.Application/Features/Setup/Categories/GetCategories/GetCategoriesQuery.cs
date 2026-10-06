using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.Categories;

[RequirePermission(PermissionFeature.Categories, PermissionLevel.Read)]
public sealed record GetCategoriesQuery(int? ParentId = null) : IRequest<GetCategoriesResponse>;
