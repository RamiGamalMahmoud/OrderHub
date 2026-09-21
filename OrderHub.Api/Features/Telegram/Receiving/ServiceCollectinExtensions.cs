using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace OrderHub.Api.Features.Telegram.Receiving;

public static class ServiceCollectinExtensions
{
    public static IServiceCollection AddReceiving(this IServiceCollection services)
    {
        var handlers = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(ITelegramUpdateHandler).IsAssignableFrom(type));

        foreach(var handler in handlers)
        {
            services.AddSingleton(typeof(ITelegramUpdateHandler), handler);
        }

        services.AddHostedService<TelegramReceiver>();
        return services;
    }
}
