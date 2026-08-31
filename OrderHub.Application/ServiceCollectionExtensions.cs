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

        return services;
    }
}
