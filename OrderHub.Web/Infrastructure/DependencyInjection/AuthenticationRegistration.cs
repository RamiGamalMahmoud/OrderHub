using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.Web.Infrastructure.DependencyInjection;

internal static class AuthenticationRegistration
{
    public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/auth/logout";

                options.Cookie.Name = "OrderHub.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                // Deliberate: SameAsRequest keeps the cookie valid over the plain-HTTP
                // LAN/offline deployment of the Windows service. Do not "fix" without
                // introducing TLS for the whole deployment.
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        return services;
    }
}
