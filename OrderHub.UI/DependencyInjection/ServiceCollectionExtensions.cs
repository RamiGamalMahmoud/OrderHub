using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application;
using OrderHub.Domain;
using OrderHub.Infrastructure.DependencyInjection;

namespace OrderHub.UI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUI(this IServiceCollection services)
    {
        services.AddDomain();
        services.AddApplication();
        services.AddInfrastructure();
        services.AddUiServices();
        services.AddViewModels();
        services.AddViews();

        return services;
    }
}