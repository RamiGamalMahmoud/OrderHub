using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Administration.Roles.GetRole;

public sealed record RolePermissionItem(
    PermissionFeature Feature,
    PermissionLevel Level);
