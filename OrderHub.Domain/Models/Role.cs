#nullable enable

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

    public bool IsSealed { get; private set; }

    public string? RoleKey { get; private set; }

    public ICollection<RolePermission> Permissions { get; private set; } = [];

    internal static Role CreateSealed(string roleKey, string name)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
            throw new ArgumentException(
                "Role key is required.",
                nameof(roleKey));

        return new Role(name)
        {
            IsSealed = true,
            RoleKey = roleKey
        };
    }

    public void Rename(string name)
    {
        EnsureNotSealed();
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
        EnsureNotSealed();

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

    private void EnsureNotSealed()
    {
        if (IsSealed)
            throw new InvalidOperationException("Role is sealed.");
    }
}