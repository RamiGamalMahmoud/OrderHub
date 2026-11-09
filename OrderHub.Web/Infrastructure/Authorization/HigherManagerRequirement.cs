using Microsoft.AspNetCore.Authorization;
using OrderHub.Web.Services;

namespace OrderHub.Web.Infrastructure.Authorization;

internal sealed record HigherManagerRequirement : IAuthorizationRequirement;

internal sealed class HigherManagerRequirementHandler(PermissionAccess access)
    : AuthorizationHandler<HigherManagerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HigherManagerRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            await access.IsHigherManagerAsync())
            context.Succeed(requirement);
    }
}
