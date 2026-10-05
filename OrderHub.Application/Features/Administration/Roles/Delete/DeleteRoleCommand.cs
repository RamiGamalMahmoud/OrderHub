using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Roles.Delete;

public sealed record DeleteRoleCommand(int RoleId) : IRequest<Result>;
