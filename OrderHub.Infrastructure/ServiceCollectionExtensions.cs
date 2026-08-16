using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.ReadServices;
using OrderHub.Infrastructure.Stores;
using System.Reflection;

namespace OrderHub.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<AppDbContextFactory>();

        services.AddServices();
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddStore();
        services.AddReadServices();
        services.AddDrafts();
        services.AddTransient<Orders.OrderWriteService>();
        services.AddSingleton<Services.WhatsappService>();
        services.AddSingleton<IWhatsappService>(s => s.GetRequiredService<Services.WhatsappService>());
        services.AddSingleton<IMessageSender>(s => s.GetRequiredService<Services.WhatsappService>());
        services.AddSingleton<IMessageService, Services.MessageService>();
        services.AddSingleton<IConnectionService, Services.ConnectionService>();

        services.AddSingleton<IDatabaseService, Services.DatabaseService>();
        services.AddHttpClient<IAuthService>();
        services.AddSingleton<IOrderService, Services.OrderService>();
        services.AddSingleton<IApplicationDirectoriesService, Services.ApplicationDirectoriesService>();
        services.AddSingleton<IAppLogger, Services.FileAppLogger>();
        services.AddSingleton<IEncryptionService, Services.EncryptionService>();
        services.AddSingleton<ICredentialsService, Services.FileCredentialsService>();
        services.AddSingleton<ITokenStorageService, Services.FieTokenStorageService>();
        services.AddTransient<IAuthService, Services.SalaAuthService>();
        services.AddSingleton<ICacheService, Services.CacheService>();
        return services;
    }
}
