using OrderHub.Application.Authorization;

namespace OrderHub.Web.Infrastructure.Authorization;

public sealed class AuthorizationDeniedMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AuthorizationDeniedException) when (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
    }
}