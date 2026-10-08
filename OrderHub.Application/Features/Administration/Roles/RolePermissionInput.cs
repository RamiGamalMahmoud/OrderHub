using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Administration.Roles;

public sealed record RolePermissionInput(
    PermissionFeature Feature,
    PermissionLevel Level);
