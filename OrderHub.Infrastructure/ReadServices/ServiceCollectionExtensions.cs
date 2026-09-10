using Microsoft.Extensions.DependencyInjection;

using OrderHub.Application.Interfaces;

namespace OrderHub.Infrastructure.ReadServices;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReadServices(this IServiceCollection services)
    {
        services.AddSingleton<ILookupService, LookupService>();
        return services;
    }
}
