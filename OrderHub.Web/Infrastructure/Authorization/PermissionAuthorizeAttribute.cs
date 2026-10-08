using Microsoft.AspNetCore.Authorization;
using OrderHub.Domain.Enums;

namespace OrderHub.Web.Infrastructure.Authorization;

public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(PermissionFeature feature, PermissionLevel level)
    {
        Policy = PolicyName(feature, level);
    }

    internal static string PolicyName(PermissionFeature feature, PermissionLevel level) => $"Permission:{feature}:{level}";
}
