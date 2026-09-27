using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Commands;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramCommandHandlers(this IServiceCollection services)
    {
        var commands = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
        type is { IsClass: true, IsAbstract: false } &&
        typeof(ITelegramCommandHandler).IsAssignableFrom(type));

        foreach (var command in commands)
        {
            services.AddScoped(typeof(ITelegramCommandHandler), command);
        }
        services.AddScoped<CommandDispatcher>();
        return services;
    }
}
