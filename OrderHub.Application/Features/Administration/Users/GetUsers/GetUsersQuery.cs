using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Users.GetUsers;

[RequirePermission(PermissionFeature.Users, PermissionLevel.Read)]
[RequireHigherManager]
public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserListItem>>;

public sealed record UserListItem(
    int Id,
    string UserName,
    string DisplayName,
    bool IsActive,
    bool IsAdmin,
    int? RoleId,
    string RoleName,
    bool IsRoleSealed,
    DateTime CreatedAt);
