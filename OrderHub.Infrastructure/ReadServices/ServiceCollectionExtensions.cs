using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.Infrastructure.ReadServices;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReadServices(this IServiceCollection services)
    {
        services.AddSingleton<Application.Interfaces.ILookupService, LookupService>();
        return services;
    }
}
