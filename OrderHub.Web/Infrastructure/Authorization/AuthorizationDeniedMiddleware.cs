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

            if (WantsDocument(context))
            {
                context.Response.Redirect("/access-denied", permanent: false);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(new AuthorizationDeniedException().Message, context.RequestAborted);
        }
    }

    /// <summary>Real browser navigations are sent to the styled /access-denied page; Blazor/HTTP callers keep the 403 status.</summary>
    private static bool WantsDocument(HttpContext context) =>
        context.Request.Headers.Accept.ToString().Contains(
            "text/html",
            StringComparison.OrdinalIgnoreCase);
}
