using System.Collections.Generic;

namespace OrderHub.Application.Features.Administration.Roles.GetRole;

public sealed record GetRoleResponse(
    int Id,
    string Name,
    IReadOnlyList<RolePermissionItem> Permissions);
