using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Users.DeactivateUser;
[RequirePermission(PermissionFeature.Users, PermissionLevel.Write)]
[RequireHigherManager]
public sealed record DeactivateUserCommand(int UserId)
    : IRequest<Result>;
