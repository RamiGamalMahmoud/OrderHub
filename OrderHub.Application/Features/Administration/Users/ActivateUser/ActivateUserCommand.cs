using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Users.ActivateUser;
[RequirePermission(PermissionFeature.Users, PermissionLevel.Write)]
[RequireHigherManager]
public sealed record ActivateUserCommand(int UserId)
    : IRequest<Result>;