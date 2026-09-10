using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Infrastructure.DependencyInjection;

internal static class IntegrationRegistration
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddHttpClient<IUpdateService, GitHubUpdateService>();
        services.AddSingleton<IWppConnectScriptService, WppConnectScriptService>();
        services.AddSingleton<PlaywrightWhatsAppService>();
        services.AddSingleton<IWhatsappService>(sp => sp.GetRequiredService<PlaywrightWhatsAppService>());
        services.AddSingleton<IMessageSender>(sp => sp.GetRequiredService<PlaywrightWhatsAppService>());
        return services;
    }
}
