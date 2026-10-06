using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Users.Update;

[RequirePermission(PermissionFeature.Users, PermissionLevel.Write)]
public record UpdateUserCommand(
    int UserId,
    UpdateUserRequest Request) : IRequest<Result>;

public record UpdateUserRequest(
    string UserName,
    string DisplayName,
    bool IsActive,
    bool IsAdmin,
    int? RoleId);