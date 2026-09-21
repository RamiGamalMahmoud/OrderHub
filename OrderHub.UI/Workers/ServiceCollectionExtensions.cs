using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.UI.Workers;
internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkers(this IServiceCollection services)
    {
        services.AddHostedService<ConnectionMonitorWorker>();
        return services;
    }
}
