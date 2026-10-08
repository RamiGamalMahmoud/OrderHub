using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using OrderHub.Application.Authorization;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OrderHub.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
        });
        return services;
    }
}
