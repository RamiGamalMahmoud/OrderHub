using OrderHub.Application.Features.Orders.GetItemEditor;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Orders.Contracts;
namespace OrderHub.Infrastructure.Features.Orders;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderReads(this IServiceCollection services)
    {
        services.AddScoped<IOrderReader, OrderReader>();
        services.AddScoped<IOrderEditorLookupReader, OrderEditorLookupReader>();
        services.AddScoped<IOrderItemEditorReader, OrderItemEditorReader>();
        return services;
    }
}
