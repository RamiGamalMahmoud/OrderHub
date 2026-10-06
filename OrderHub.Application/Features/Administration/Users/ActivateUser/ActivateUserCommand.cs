using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Users.ActivateUser;
[RequirePermission(PermissionFeature.Users, PermissionLevel.Write)]
public sealed record ActivateUserCommand(int UserId)
    : IRequest<Result>;