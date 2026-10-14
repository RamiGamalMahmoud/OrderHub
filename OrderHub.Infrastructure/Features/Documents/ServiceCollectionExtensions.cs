using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Documents.GetDocuments;

namespace OrderHub.Infrastructure.Features.Documents;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentReads(this IServiceCollection services)
    {
        services.AddScoped<IDocumentReader, DocumentReader>();
        return services;
    }
}
