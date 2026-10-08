using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Users.Create;
[RequirePermission(PermissionFeature.Users, PermissionLevel.Write)]
public record CreateUserCommand(CreateUserRequest Request) : IRequest<Result>;

public record CreateUserRequest(
    string UserName,
    string DisplayName,
    string Password,
    bool IsActive,
    bool IsAdmin,
    int? RoleId);
