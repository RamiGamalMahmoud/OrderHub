using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Roles.Update;

public record UpdateRoleCommand(int RoleId, UpdateRoleRequest Request) : IRequest<Result>;
