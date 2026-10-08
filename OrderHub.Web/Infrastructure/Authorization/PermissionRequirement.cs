using Microsoft.AspNetCore.Authorization;
using OrderHub.Domain.Enums;
using OrderHub.Web.Services;

namespace OrderHub.Web.Infrastructure.Authorization;

internal sealed record PermissionRequirement(PermissionFeature Feature, PermissionLevel Level) : IAuthorizationRequirement;

internal sealed class PermissionRequirementHandler(PermissionAccess access) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            await access.IsAllowedAsync(requirement.Feature, requirement.Level))
            context.Succeed(requirement);
    }
}
