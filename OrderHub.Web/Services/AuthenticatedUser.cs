using Microsoft.AspNetCore.Components.Authorization;
using OrderHub.Application.Context;
using System.Security.Claims;

namespace OrderHub.Web.Services;

public sealed class AuthenticatedUser(
    AuthenticationStateProvider authenticationStateProvider,
    IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public async Task<int?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal principal;
        try
        {
            principal = (await authenticationStateProvider.GetAuthenticationStateAsync()
                .WaitAsync(cancellationToken)).User;
        }
        catch (InvalidOperationException) when (httpContextAccessor.HttpContext is not null)
        {
            // Controller requests do not have a Blazor circuit authentication state.
            principal = httpContextAccessor.HttpContext.User;
        }

        return principal.Identity?.IsAuthenticated == true
            && int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            && id > 0 ? id : null;
    }
}
