using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace OrderHub.Web.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult result)
    {
        if (result.Forbidden &&
            policy.Requirements.OfType<PermissionRequirement>().Any())
        {
            context.Response.Redirect("/access-denied");
            return;
        }

        await _defaultHandler.HandleAsync(
            next,
            context,
            policy,
            result);
    }
}