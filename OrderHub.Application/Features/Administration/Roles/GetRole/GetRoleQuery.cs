using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;

namespace OrderHub.Application.Features.Administration.Roles.GetRole;

[RequirePermission(PermissionFeature.Users, PermissionLevel.Read)]
public sealed record GetRoleQuery(int RoleId) : IRequest<GetRoleResponse>;
