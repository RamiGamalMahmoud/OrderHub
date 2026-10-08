using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

[RequirePermission(PermissionFeature.Categories, PermissionLevel.Write)]
public record DeleteCategoryCommand(int Id) : IRequest<Result>;
