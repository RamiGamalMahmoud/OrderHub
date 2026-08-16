using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.Infrastructure.Stores;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStore(this IServiceCollection services)
    {
        services.AddSingleton<Application.Features.Orders.Contracts.IOrderStore, OrderStore>();
        services.AddSingleton<Application.Interfaces.Stores.IPropertyStore, PropertyStore>();
        services.AddSingleton<Application.Features.Products.Contracts.IProductStore, ProductStore>();
        services.AddTransient<Application.Features.Orders.GetOrderItemEditor.IOrderItemEditorReader, Features.Orders.OrderItemEditorReader>();
        return services;
    }
}
