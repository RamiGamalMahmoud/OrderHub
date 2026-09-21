using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Services;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Infrastructure.DependencyInjection;

internal static class AuthenticationRegistration
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddSingleton<IHashShervice, HashShervice>();
        services.AddTransient<IUserSessionStorage, UserSessionStorage>();
        services.AddTransient<IUserSessionValidator, UserSessionValidator>();
        services.AddTransient<IInitialAdminService, InitialAdminService>();
        services.AddHttpClient<IAuthService, SalaAuthService>();
        return services;
    }
}
