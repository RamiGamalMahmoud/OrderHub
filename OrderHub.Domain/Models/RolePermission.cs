using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Domain.Models;

public class RolePermission
{
    private RolePermission()
    {
    }

    internal RolePermission(
        Role role,
        PermissionFeature feature,
        PermissionLevel level)
    {
        ArgumentNullException.ThrowIfNull(role);

        ValidateLevel(level);

        Role = role;
        Feature = feature;
        Level = level;
    }

    public int RoleId { get; private set; }

    public PermissionFeature Feature { get; private set; }

    public PermissionLevel Level { get; private set; }

    public Role Role { get; private set; } = null!;

    internal void ChangeLevel(PermissionLevel level)
    {
        ValidateLevel(level);

        Level = level;
    }

    private static void ValidateLevel(PermissionLevel level)
    {
        if (level == PermissionLevel.None)
            throw new ArgumentException(
                "Role permission level cannot be None.",
                nameof(level));
    }
}