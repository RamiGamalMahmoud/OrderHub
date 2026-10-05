using MediatR;
using System;

namespace OrderHub.Application.Features.Administration.Users.GetUser;

public sealed record GetUserQuery(int UserId)
    : IRequest<GetUserResponse>;

public sealed record GetUserResponse(
    int Id,
    string UserName,
    string DisplayName,
    bool IsActive,
    bool IsAdmin,
    DateTime CreatedAt);
