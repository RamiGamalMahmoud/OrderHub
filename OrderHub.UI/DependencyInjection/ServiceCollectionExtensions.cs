using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application;
using OrderHub.Application.Options;
using OrderHub.Domain;
using OrderHub.Infrastructure.DependencyInjection;
using OrderHub.UI.Commands;
using OrderHub.UI.Workers;

namespace OrderHub.UI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUI(this IServiceCollection services)
    {
        services.AddDomain();
        services.AddApplication();
        services.AddInfrastructure();
        services.AddUiServices();
        services.AddViewModels();
        services.AddViews();
        services.AddWorkers();

        services.Configure<TelegramLinkOptions>(options =>
        {
            options.BotUsername = "OrderHubMessagingBot";
            options.LinkExpirationMinutes = 1440;
        });

        return services;
    }
}