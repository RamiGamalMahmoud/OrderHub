using Microsoft.AspNetCore.Components.Authorization;
using OrderHub.Application.Authorization;
using System.Security.Claims;

namespace OrderHub.Web.Services;

public sealed class AuthenticatedUser(
    AuthenticationStateProvider authenticationStateProvider,
    IHttpContextAccessor httpContextAccessor,
    IUserAdminReader userAdminReader)
{
    public async Task<CurrentUserIdentity> GetIdentityAsync(CancellationToken cancellationToken = default)
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

        if (principal.Identity?.IsAuthenticated != true)
            return new CurrentUserIdentity(null, null, null, false, false);

        int? userId = int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            && id > 0 ? id : null;

        // Higher Manager authority is read from the database on every operation so that
        // promotion and demotion take effect immediately for the edited user's own session.
        bool isAdmin = userId is > 0
            && await userAdminReader.IsAdminAsync(userId.Value, cancellationToken);

        return new CurrentUserIdentity(userId, principal.FindFirstValue(ClaimTypes.Name),
            principal.FindFirstValue("DisplayName"), true, isAdmin);
    }
}
