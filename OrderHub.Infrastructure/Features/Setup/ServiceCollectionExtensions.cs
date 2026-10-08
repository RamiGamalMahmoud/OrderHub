using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.Infrastructure.Features.Setup;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSetupPersistence(this IServiceCollection services)
    {
        services.AddScoped<OrderHub.Application.Features.Setup.Cities.Contracts.ICityReader, OrderHub.Infrastructure.Features.Setup.Cities.CityReader>();
        services.AddScoped<OrderHub.Application.Features.Setup.Categories.Contracts.ICategoryReader, OrderHub.Infrastructure.Features.Setup.Categories.CategoryReader>();
        services.AddScoped<OrderHub.Application.Features.Setup.Clients.Contracts.IClientReader, OrderHub.Infrastructure.Features.Setup.Clients.ClientReader>();
        services.AddScoped<OrderHub.Application.Features.Setup.Deliverymen.Contracts.IDeliverymanReader, OrderHub.Infrastructure.Features.Setup.Deliverymen.DeliverymanReader>();
        services.AddScoped<OrderHub.Application.Features.Setup.ShippingCarriers.Contracts.IShippingCarrierReader, OrderHub.Infrastructure.Features.Setup.ShippingCarriers.ShippingCarrierReader>();
        services.AddScoped<OrderHub.Application.Features.Setup.Suppliers.Contracts.ISupplierReader, OrderHub.Infrastructure.Features.Setup.Suppliers.SupplierReader>();
        services.AddScoped<OrderHub.Application.Features.Setup.PaymentMethods.Contracts.IPaymentMethodReader, OrderHub.Infrastructure.Features.Setup.PaymentMethods.PaymentMethodReader>();
        services.AddScoped<OrderHub.Application.Features.Setup.Cities.Contracts.ICityRepository, OrderHub.Infrastructure.Features.Setup.Cities.CityRepository>();
        services.AddScoped<OrderHub.Application.Features.Setup.Categories.Contracts.ICategoryRepository, OrderHub.Infrastructure.Features.Setup.Categories.CategoryRepository>();
        services.AddScoped<OrderHub.Application.Features.Setup.Clients.Contracts.IClientRepository, OrderHub.Infrastructure.Features.Setup.Clients.ClientRepository>();
        services.AddScoped<OrderHub.Application.Features.Setup.Deliverymen.Contracts.IDeliverymanRepository, OrderHub.Infrastructure.Features.Setup.Deliverymen.DeliverymanRepository>();
        services.AddScoped<OrderHub.Application.Features.Setup.ShippingCarriers.Contracts.IShippingCarrierRepository, OrderHub.Infrastructure.Features.Setup.ShippingCarriers.ShippingCarrierRepository>();
        services.AddScoped<OrderHub.Application.Features.Setup.Suppliers.Contracts.ISupplierRepository, OrderHub.Infrastructure.Features.Setup.Suppliers.SupplierRepository>();
        return services;
    }
}
