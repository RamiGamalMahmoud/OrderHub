using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using System.Linq;

namespace OrderHub.Application.Services;
public class AuthorizationService : IAuthorizationService
{
    public bool CanRead(PermissionFeature feature, UserSession user)
    {
        if (user is null)
            return false;

        if (user.IsAdmin)
            return true;

        return user.Permissions.Any(p => p.Feature == feature && p.CanRead);
    }

    public bool CanWrite(PermissionFeature feature, UserSession user)
    {
        if (user is null)
            return false;

        if (user.IsAdmin)
            return true;

        return user.Permissions.Any(p => p.Feature == feature && p.CanWrite);
    }
}
