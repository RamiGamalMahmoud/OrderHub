using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OrderHub.Domain.Models;

public class User : ModelBase
{
    public string UserName { get; private set; }
    public string DisplayName { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAdmin { get; private set; }

    private readonly Collection<UserPermission> _permissions = [];

    public IReadOnlyCollection<UserPermission> Permissions => _permissions;

    private User()
    {
    }

    public static User Create(
        string userName,
        string passwordHash,
        string displayName,
        bool isActive = true,
        bool isAdmin = false)
    {
        ValidateUserName(userName);
        ValidateDisplayName(displayName);
        ValidatePasswordHash(passwordHash);

        return new User
        {
            UserName = userName.Trim(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            IsActive = isActive,
            IsAdmin = isAdmin
        };
    }

    public void ChangeUserName(string userName)
    {
        ValidateUserName(userName);

        UserName = userName.Trim();
    }

    public void ChangeDisplayName(string displayName)
    {
        ValidateDisplayName(displayName);

        DisplayName = displayName.Trim();
    }

    public void ChangePassword(string passwordHash)
    {
        ValidatePasswordHash(passwordHash);

        PasswordHash = passwordHash;
    }

    public void SetAdmin(bool isAdmin)
    {
        IsAdmin = isAdmin;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void SetPermission(
        PermissionFeature feature,
        bool canRead,
        bool canWrite)
    {
        UserPermission permission = _permissions
            .FirstOrDefault(x => x.Feature == feature);

        if (permission is null)
        {
            _permissions.Add(
                UserPermission.Create(
                    this,
                    feature,
                    canRead,
                    canWrite));

            return;
        }

        permission.SetPermissions(canRead, canWrite);
    }

    public UserPermission GetPermission(PermissionFeature feature)
    {
        return _permissions
            .FirstOrDefault(x => x.Feature == feature);
    }

    public void RemovePermission(PermissionFeature feature)
    {
        UserPermission permission = _permissions
            .FirstOrDefault(x => x.Feature == feature);

        if (permission is null)
            return;

        _permissions.Remove(permission);
    }

    private static void ValidateUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException(
                "Username is required.",
                nameof(userName));
    }

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException(
                "Display name is required.",
                nameof(displayName));
    }

    private static void ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "Password hash is required.",
                nameof(passwordHash));
    }

    public void ClearPermissions() => _permissions.Clear();
}