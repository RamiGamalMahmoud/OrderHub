using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Users.Create;
public record CreateUserCommand(CreateUserRequest Request) : IRequest<Result>;

public record CreateUserRequest(
    string UserName,
    string DisplayName,
    string Password,
    bool IsActive,
    bool IsAdmin,
    IReadOnlyList<CreateUserRequestPermission> Permissions);

public record CreateUserRequestPermission(
    PermissionFeature Feature,
    bool CanRead,
    bool CanWrite);