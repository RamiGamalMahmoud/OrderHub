using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Infrastructure.Connection;
using OrderHub.Application.Infrastructure.Installation;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Documents;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure.Application.Connection;
using OrderHub.Infrastructure.Application.Installation;
using OrderHub.Infrastructure.Configuration;
using OrderHub.Infrastructure.Execution;
using OrderHub.Infrastructure.Features.Documents;
using OrderHub.Infrastructure.Orders;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Infrastructure.DependencyInjection;

internal static class ServicesRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IDatabaseConfigurationService, DatabaseConfigurationService>();
        services.AddSingleton<IRequestExecutor, RequestExecutor>();
        services.AddSingleton<IPdfDocumentFactory, PdfDocumentFactory>();
        //services.AddSingleton<ILocalFileAccessService, LocalFileAccessService>();
        services.AddSingleton<IFileStorageService, RemoteFileStorageService>();
        services.AddTransient<OrderWriteService>();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<IApplicationDirectoriesService, ApplicationDirectoriesService>();
        services.AddSingleton<IAppLogger, FileAppLogger>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<ICredentialsService, FileCredentialsService>();
        services.AddSingleton<ITokenStorageService, FieTokenStorageService>();
        services.AddSingleton<IConnectionConfigurationStorage, DpapiConnectionConfigurationStorage>();
        services.AddSingleton<IApplicationConfigurationProvider, ApplicationConfigurationProvider>();
        services.AddSingleton<IServerConfiguration, ServerConfiguration>();
        return services;
    }
}
