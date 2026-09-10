using Microsoft.Extensions.DependencyInjection;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.Queries;
using OrderHub.Infrastructure.ReadServices;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Stores;
using System.Reflection;

namespace OrderHub.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddPersistence();
        services.AddStores();
        services.AddRepositories();
        services.AddQueries();
        services.AddReadServices();
        services.AddDrafts();

        services.AddAuthenticationServices();
        services.AddMessaging();
        services.AddIntegrations();
        services.AddServices();

        return services;
    }
}
