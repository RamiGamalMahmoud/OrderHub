using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using System;

namespace OrderHub.Application.Features.Administration.Users.GetUser;

[RequirePermission(PermissionFeature.Users, PermissionLevel.Read)]
public sealed record GetUserQuery(int UserId)
    : IRequest<GetUserResponse>;

public sealed record GetUserResponse(
    int Id,
    string UserName,
    string DisplayName,
    bool IsActive,
    bool IsAdmin,
    int? RoleId,
    string RoleName,
    DateTime CreatedAt);