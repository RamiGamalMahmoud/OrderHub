using MediatR;

namespace OrderHub.Application.Features.Administration.Roles.GetRole;

public sealed record GetRoleQuery(int RoleId) : IRequest<GetRoleResponse>;
