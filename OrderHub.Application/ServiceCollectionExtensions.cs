using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<IOrderEntitySequenceService, OrderEntitySequenceService>();

        return services;
    }
}
