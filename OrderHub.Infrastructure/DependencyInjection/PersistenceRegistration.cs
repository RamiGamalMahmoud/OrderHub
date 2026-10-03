using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Infrastructure.DependencyInjection;

internal static class PersistenceRegistration
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<AppDbContextFactory>(sp => new AppDbContextFactory(connectionString));
        services.AddScoped(sp => sp.GetRequiredService<AppDbContextFactory>().CreateDbContext());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
