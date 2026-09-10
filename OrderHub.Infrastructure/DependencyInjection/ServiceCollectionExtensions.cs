using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Services;

//using OrderHub.Infrastructure.Application.Connection;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.Queries;
using OrderHub.Infrastructure.ReadServices;
using OrderHub.Infrastructure.Services;
using OrderHub.Infrastructure.Stores;
using System.Reflection;

namespace OrderHub.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<AppDbContextFactory>();
        services.AddScoped(services =>
        {
            return services.GetRequiredService<AppDbContextFactory>().CreateDbContext();
        });

        services.AddServices();
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddStore();
        services.AddQueries();
        services.AddReadServices();
        services.AddDrafts();
        services.AddSingleton<IHashShervice, HashShervice>();
        services.AddTransient<IUserSessionStorage, UserSessionStorage>();
        services.AddTransient<IUserSessionValidator, UserSessionValidator>();
        services.AddTransient<IInitialAdminService, InitialAdminService>();

        services.AddSingleton<OrderHub.Application.Infrastructure.Connection.IConnectionConfigurationStorage, 
            OrderHub.Infrastructure.Application.Connection.DpapiConnectionConfigurationStorage>();

        services.AddSingleton<OrderHub.Application.Infrastructure.Installation.IApplicationConfigurationProvider, 
            Application.Installation.ApplicationConfigurationProvider>();

        services.AddTransient<Orders.OrderWriteService>();
        services.AddSingleton<IDatabaseConfigurationService, Configuration.DatabaseConfigurationService>();
        services.AddSingleton<IWppConnectScriptService, Services.WppConnectScriptService>();
        services.AddSingleton<IRequestExecutor, Execution.RequestExecutor>();

        services.AddSingleton<OrderHub.Application.Interfaces.Documents.IPdfDocumentFactory, Features.Documents.PdfDocumentFactory>();
        services.AddSingleton<OrderHub.Application.Interfaces.ILocalFileAccessService, Services.LocalFileAccessService>();
        services.AddSingleton<IFileStorageService, Services.LocalFileStorageService>();
        services.AddSingleton<Services.PlaywrightWhatsAppService>();
        services.AddSingleton<IWhatsappService>(s => s.GetRequiredService<Services.PlaywrightWhatsAppService>());
        services.AddSingleton<IMessageSender>(s => s.GetRequiredService<Services.PlaywrightWhatsAppService>());
        services.AddScoped<IUnitOfWork, Services.UnitOfWork>();
        services.AddSingleton<IMessageService, Services.MessageService>();

        services.AddSingleton<Services.MessageSendingOptions>(s => new Services.MessageSendingOptions());

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
