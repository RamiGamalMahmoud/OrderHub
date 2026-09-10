using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Infrastructure.DependencyInjection;

internal static class MessagingRegistration
{
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<MessageSendingOptions>();
        return services;
    }
}
