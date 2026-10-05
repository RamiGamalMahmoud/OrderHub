using System;

namespace OrderHub.Domain.Models;

public class User : ModelBase
{
    public string UserName { get; private set; }
    public string DisplayName { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAdmin { get; private set; }

    public int? RoleId { get; private set; }

    public Role Role { get; private set; }

    public void AssignRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        Role = role;
    }

    public void RemoveRole()
    {
        Role = null;
        RoleId = null;
    }


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

}