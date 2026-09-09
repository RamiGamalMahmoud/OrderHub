using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace OrderHub.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddMemoryCache();

        services.AddSingleton<Interfaces.Services.ISessionManager, Services.SessionManager>();
        services.AddScoped<Interfaces.Services.IOrderEntitySequenceService, Services.OrderEntitySequenceService>();
        services.AddSingleton<Context.IApplicationContext, Context.ApplicationContext>();
        services.AddHttpClient("OrderHubApi", client =>
        {
            client.BaseAddress = new System.Uri("http://100.65.6.36:5000/api/");
        });

        return services;
    }
}
