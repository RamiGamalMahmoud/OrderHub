using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Administration.Roles.Create;

public record CreateRoleCommand(CreateRoleRequest Request) : IRequest<Result>;
