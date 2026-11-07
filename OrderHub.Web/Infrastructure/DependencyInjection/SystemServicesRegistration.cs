using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.Web.Infrastructure.DependencyInjection;

internal static class SystemServicesRegistration
{
    public static IServiceCollection AddSystemServices(this IServiceCollection services)
    {
        services.AddWindowsService(options =>
        {
            options.ServiceName = "OrderHubWeb";
        });

        services.AddControllers();
        services.AddHealthChecks();

        return services;
    }
}
