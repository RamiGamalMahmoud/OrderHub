using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Roles.Create;

public record CreateRoleRequest(
    string Name,
    IReadOnlyList<RolePermissionInput> Permissions);
