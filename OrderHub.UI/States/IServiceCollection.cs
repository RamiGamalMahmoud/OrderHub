using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace OrderHub.UI.States;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStates(this IServiceCollection services)
    {
        services.AddSingleton<AppState>();
        services.AddSingleton<IAppState>(sp => sp.GetRequiredService<AppState>());

        var stateTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(IState).IsAssignableFrom(type));

        foreach (Type type in stateTypes)
        {
            services.AddSingleton(type);
        }

        return services;
    }
}
