using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Users.Create;
public record CreateUserCommand(CreateUserRequest Request) : IRequest<Result>;

public record CreateUserRequest(
    string UserName,
    string DisplayName,
    string Password,
    bool IsActive,
    bool IsAdmin,
    int? RoleId);
