using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Users.ActivateUser;
public sealed record ActivateUserCommand(int UserId)
    : IRequest<Result>;