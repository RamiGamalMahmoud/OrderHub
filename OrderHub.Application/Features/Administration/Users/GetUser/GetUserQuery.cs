using MediatR;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Users.GetUser;

public sealed record GetUserQuery(int UserId)
    : IRequest<GetUserResponse>;

public sealed record GetUserResponse(
    int Id,
    string UserName,
    string DisplayName,
    bool IsActive,
    bool IsAdmin,
    IReadOnlyList<UserListItemPermission> Permissions,
    DateTime CreatedAt);

public sealed record UserListItemPermission(
    int Id,
    PermissionFeature PermissionFeature,
    bool CanRead,
    bool CanWrite);