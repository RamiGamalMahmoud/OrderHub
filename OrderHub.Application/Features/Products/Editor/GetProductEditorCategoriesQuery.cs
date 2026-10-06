using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Products.Editor;

[RequirePermission(PermissionFeature.Products, PermissionLevel.Write)]
public sealed record GetProductEditorCategoriesQuery(int? ParentId = null) : IRequest<GetCategoriesResponse>;
