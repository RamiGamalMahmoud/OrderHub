using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Users.Update;
public record UpdateUserCommand(
    int UserId,
    UpdateUserRequest Request) : IRequest<Result>;

public record UpdateUserRequest(
    string UserName,
    string DisplayName,
    bool IsActive,
    bool IsAdmin,
    IReadOnlyList<UpdateUserRequestPermission> Permissions);

public record UpdateUserRequestPermission(
    PermissionFeature Feature,
    bool CanRead,
    bool CanWrite);