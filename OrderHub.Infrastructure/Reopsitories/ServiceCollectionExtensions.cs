using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Infrastructure.Stores;

namespace OrderHub.Infrastructure.Reopsitories;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IDocumentSequenceRepository, DocumentSequenceRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        services.AddScoped<IProformaInvoiceRepository, ProformaInvoiceRepository>();
        services.AddScoped<ITelegramAccountRepository, TelegramAccountRepository>();
        services.AddScoped<ITelegramLinkRepository, TelegramLinkRepository>();
        services.AddScoped<IDeliverymanRepository, DeliverymanRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        return services;
    }
}
