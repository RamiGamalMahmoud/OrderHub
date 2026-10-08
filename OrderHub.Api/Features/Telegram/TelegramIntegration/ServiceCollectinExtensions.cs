using Microsoft.Extensions.DependencyInjection;
using OrderHub.Api.Features.Telegram.TelegramIntegration.Commands;
using OrderHub.Api.Features.Telegram.TelegramIntegration.Routing;
using System.Linq;
using System.Reflection;
using Telegram.Bot.Polling;

namespace OrderHub.Api.Features.TelegramIntegration;

public static class ServiceCollectinExtensions
{
    public static IServiceCollection AddReceiving(this IServiceCollection services)
    {
        var handlers = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(IUpdateRouteHandler).IsAssignableFrom(type));

        foreach(var handler in handlers)
        {
            services.AddScoped(typeof(IUpdateRouteHandler), handler);
        }

        services.AddTelegramCommandHandlers();

        services.AddSingleton<IUpdateHandler, UpdateHandler>();
        services.AddHostedService<TelegramReceiver>();
        return services;
    }
}
