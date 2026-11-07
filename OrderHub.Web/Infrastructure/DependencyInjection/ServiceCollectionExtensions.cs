using Microsoft.Extensions.DependencyInjection;
using OrderHub.Web.Infrastructure.Authorization;

namespace OrderHub.Web.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers every Web-layer service: interactive components, host/system services, cookie authentication and permission authorization.</summary>
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddComponents();
        services.AddSystemServices();
        services.AddCookieAuthentication();
        services.AddPermissionAuthorization();

        return services;
    }
}
