using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using System;
using System.Linq;

namespace OrderHub.Application.Services;
internal class AuthorizationService : IAuthorizationService
{
    private readonly IApplicationContext _applicationContext;

    public AuthorizationService(IApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
        _applicationContext.UserChanged += OnUserChanged;
    }

    private void OnUserChanged(object sender, EventArgs e)
    {
        OnAthorizationChanged(this, EventArgs.Empty);
    }

    private void OnAthorizationChanged(object sender, EventArgs e)
    {
        AuthorizationChanged?.Invoke(sender, e);
    }

    public event EventHandler AuthorizationChanged;

    public bool CanRead(PermissionFeature feature)
    {
        UserSession user = _applicationContext.User;

        if (user is null)
            return false;

        if (user.IsAdmin)
            return true;

        return user.Permissions.Any(p => p.Feature == feature && p.CanRead);
    }

    public bool CanWrite(PermissionFeature feature)
    {
        UserSession user = _applicationContext.User;

        if (user is null)
            return false;

        if (user.IsAdmin)
            return true;

        return user.Permissions.Any(p => p.Feature == feature && p.CanWrite);
    }
}
