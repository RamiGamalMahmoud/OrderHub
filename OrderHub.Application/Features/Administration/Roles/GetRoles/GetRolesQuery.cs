using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Roles.GetRoles;

[RequirePermission(PermissionFeature.Users, PermissionLevel.Read)]
public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleListItem>>;
