using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Infrastructure.Features.Orders;

namespace OrderHub.Infrastructure.Stores;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStores(this IServiceCollection services)
    {
        services.AddSingleton<IPropertyStore, PropertyStore>();
        services.AddSingleton<IProductStore, ProductStore>();
        services.AddTransient<GetOrderItemEditor.IOrderItemEditorReader, OrderItemEditorReader>();
        return services;
    }
}
