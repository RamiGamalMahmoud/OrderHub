using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Users.DeactivateUser;
public sealed record DeactivateUserCommand(int UserId)
    : IRequest<Result>;
