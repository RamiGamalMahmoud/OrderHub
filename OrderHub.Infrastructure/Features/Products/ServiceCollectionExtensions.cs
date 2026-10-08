using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Infrastructure.Features.Setup.Properties;

namespace OrderHub.Infrastructure.Features.Products;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogPersistence(this IServiceCollection services)
    {
        services.AddScoped<IProductReader, ProductReader>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPropertyReader, PropertyReader>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        return services;
    }
}
