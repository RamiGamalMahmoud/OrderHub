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
            var isController = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>() is not null;
            if (!isController && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
                context.Response.Redirect("/access-denied");
            else
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(new OrderHub.Application.Authorization.AuthorizationDeniedException().Message, context.RequestAborted);
            }
            return;
        }

        await _defaultHandler.HandleAsync(
            next,
            context,
            policy,
            result);
    }
}
