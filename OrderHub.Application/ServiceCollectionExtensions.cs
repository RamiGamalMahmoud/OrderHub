using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Services;
using System.Reflection;

namespace OrderHub.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddScoped<IOrderEntitySequenceService, OrderEntitySequenceService>();
        services.AddSingleton<IApplicationContext, ApplicationContext>();

        services.AddHttpClient("OrderHubApi", (sp, client) =>
        {
            IServerConfiguration serverConfiguration = sp.GetRequiredService<IServerConfiguration>();
            client.BaseAddress = new System.Uri(serverConfiguration.ApiBaseUrl);
        });

        return services;
    }
}
