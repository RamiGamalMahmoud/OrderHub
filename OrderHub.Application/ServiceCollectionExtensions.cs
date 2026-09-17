using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
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
        services.AddMemoryCache();
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddScoped<IOrderEntitySequenceService, OrderEntitySequenceService>();
        services.AddSingleton<IApplicationContext, ApplicationContext>();
        services.AddHttpClient("OrderHubApi", (sp, client) =>
        {
            IApplicationContext context = sp.GetService<IApplicationContext>();
            string serverAddress = context.Application.InstallationType switch
            {
                Infrastructure.Installation.InstallationType.Client => GetServerAddress(),
                _ => "localhost"
            };
            string baseUrl = $"http://{serverAddress}:5000/api/";
            client.BaseAddress = new System.Uri(baseUrl);
        });

        return services;
    }

    public static string GetServerAddress()
    {
        const string registryPath = @"SOFTWARE\Almasajid\OrderHub";
        const string valueName = "ServerAddress";

        using RegistryKey baseKey = RegistryKey.OpenBaseKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64);

        using RegistryKey key = baseKey.OpenSubKey(registryPath);

        return key?.GetValue(valueName)?.ToString();
    }
}
