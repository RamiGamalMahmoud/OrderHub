using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.Domain;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        return services;
    }
}
