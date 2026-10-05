using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Administration.Users.GetUser;
using OrderHub.Application.Features.Administration.Users.GetUsers;
using OrderHub.Infrastructure.Features.Administration.Users;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Features.Orders;
using OrderHub.Infrastructure.Features.Products;
using OrderHub.Infrastructure.Features.Setup;

namespace OrderHub.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddPersistence(connectionString);
        services.AddOrderReads();
        services.AddRepositories();
        services.AddDrafts();
        services.AddSetupPersistence();
        services.AddCatalogPersistence();

        services.AddServices();

        services.AddTransient<UserReadService>();

        services.AddTransient<IUserListingService>(sp => sp.GetRequiredService<UserReadService>());

        services.AddTransient<IUserDetailsService>(provider => provider.GetRequiredService<UserReadService>());

        return services;
    }
}
