using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Infrastructure.Repositories;

namespace OrderHub.Infrastructure.Reopsitories;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IDocumentSequenceRepository, DocumentSequenceRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        services.AddScoped<IProformaInvoiceRepository, ProformaInvoiceRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        return services;
    }
}
