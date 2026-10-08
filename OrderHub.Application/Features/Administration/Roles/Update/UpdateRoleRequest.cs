using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Roles.Update;

public record UpdateRoleRequest(
    string Name,
    IReadOnlyList<RolePermissionInput> Permissions);
