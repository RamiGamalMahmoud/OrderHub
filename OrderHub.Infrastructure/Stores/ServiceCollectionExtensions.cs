using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Infrastructure.Reopsitories;

namespace OrderHub.Infrastructure.Stores;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStore(this IServiceCollection services)
    {
        services.AddSingleton<OrderHub.Application.Features.Orders.Contracts.IOrderStore, OrderStore>();
        services.AddSingleton<IPropertyStore, PropertyStore>();
        services.AddSingleton<OrderHub.Application.Features.Products.Contracts.IProductStore, ProductStore>();
        services.AddTransient<OrderHub.Application.Features.Orders.GetOrderItemEditor.IOrderItemEditorReader, Features.Orders.OrderItemEditorReader>();
        services.AddScoped<IDocumentSequenceRepository, DocumentSequenceRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        services.AddScoped<IProformaInvoiceRepository, ProformaInvoiceRepository>();
        return services;
    }
}
