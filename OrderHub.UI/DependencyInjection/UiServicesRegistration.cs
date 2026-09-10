using Microsoft.Extensions.DependencyInjection;
using Notification.Wpf;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using OrderHub.UI.Services.ApiServer;
using OrderHub.UI.StartUpSteps;

namespace OrderHub.UI.DependencyInjection;
public static class UiServicesRegistration
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddStartUp();

        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<StartupProgress>();
        services.AddSingleton<Properties.Settings>();

        services.AddSingleton<INotificationManager, NotificationManager>();

        services.AddSingleton<IApplicationInitializationService, ApplicationInitializationService>();
        services.AddSingleton<IApiServerService, EmbeddedApiServerService>();

        return services;
    }
}