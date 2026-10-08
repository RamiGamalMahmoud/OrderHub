using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Documents;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure.Features.Documents;
using OrderHub.Infrastructure.Services;
using OrderHub.Infrastructure.Services.FilesAccess;

namespace OrderHub.Infrastructure.DependencyInjection;

internal static class ServicesRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IPdfDocumentFactory, PdfDocumentFactory>();
        services.AddSingleton<ILocalFileAccessService, LocalFileAccessService>();
        services.AddSingleton<IApplicationDirectoriesService, ApplicationDirectoriesService>();
        services.AddSingleton<IAppLogger, FileAppLogger>();
        services.AddSingleton<IHashShervice, HashShervice>();
        return services;
    }
}
