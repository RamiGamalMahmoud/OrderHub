using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace OrderHub.UI.Commands;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        services.AddSingleton<AppCommands>();
        services.AddSingleton<IAppCommands>(sp => sp.GetRequiredService<AppCommands>());

        var commandTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(ICommandGroup).IsAssignableFrom(type));

        foreach (Type type in commandTypes)
        {
            services.AddSingleton(type);
        }

        return services;
    }
}