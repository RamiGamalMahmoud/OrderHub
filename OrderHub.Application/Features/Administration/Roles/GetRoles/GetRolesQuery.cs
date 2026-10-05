using MediatR;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Roles.GetRoles;

public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleListItem>>;
