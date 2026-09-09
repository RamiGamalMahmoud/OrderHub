using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrderHub.Domain.Models;
public class User : ModelBase
{
    public string UserName { get; private set; }
    public string DisplayName { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }

    private Collection<string> _roles = [];
    public IReadOnlyCollection<string> Roles => _roles;

    private User() { }
    public static User Create(
        string userName,
        string passwordHash,
        string displayName,
        bool isActive)
    {
        return new User()
        {
            UserName = userName,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            IsActive = isActive
        };
    }

    public void AddRole(string role)
    {
        if (_roles.Contains(role))
            return;

        _roles.Add(role);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
