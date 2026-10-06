using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Roles.Delete;

[RequirePermission(PermissionFeature.Users, PermissionLevel.Write)]
public sealed record DeleteRoleCommand(int RoleId) : IRequest<Result>;
