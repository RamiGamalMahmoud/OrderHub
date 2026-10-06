using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Roles.Create;

[RequirePermission(PermissionFeature.Users, PermissionLevel.Write)]
public record CreateRoleCommand(CreateRoleRequest Request) : IRequest<Result>;
