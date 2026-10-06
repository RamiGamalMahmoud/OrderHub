using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Roles.Update;

[RequirePermission(PermissionFeature.Users, PermissionLevel.Write)]
public record UpdateRoleCommand(int RoleId, UpdateRoleRequest Request) : IRequest<Result>;
