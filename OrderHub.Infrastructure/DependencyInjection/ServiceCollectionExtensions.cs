using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Administration.Users.GetUser;
using OrderHub.Application.Features.Administration.Users.GetUsers;
using OrderHub.Infrastructure.Features.Administration;
using OrderHub.Infrastructure.Features.Administration.Users;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.Features.Orders;
using OrderHub.Infrastructure.Features.Products;
using OrderHub.Infrastructure.Features.Setup;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Application.Authorization;

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

        services.AddUserReadService();
        services.AddRoleReadService();
        services.AddScoped<IUserPermissionReader, UserPermissionReader>();


        return services;
    }
}
