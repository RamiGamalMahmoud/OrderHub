using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public class Role : ModelBase
{
    private Role()
    {
    }

    public Role(string name)
    {
        SetName(name);
    }

    public string Name { get; private set; } = null!;

    public ICollection<RolePermission> Permissions { get; private set; } = [];

    public void Rename(string name)
    {
        SetName(name);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Role name is required.",
                nameof(name));

        Name = name.Trim();
    }

    public void SetPermission(
        PermissionFeature feature,
        PermissionLevel level)
    {
        var permission = Permissions
            .FirstOrDefault(x => x.Feature == feature);

        if (level == PermissionLevel.None)
        {
            if (permission is not null)
                Permissions.Remove(permission);

            return;
        }

        if (permission is null)
        {
            Permissions.Add(
                new RolePermission(this, feature, level));

            return;
        }

        permission.ChangeLevel(level);
    }
}