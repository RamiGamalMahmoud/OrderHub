using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Administration.Users.GetUser;
using OrderHub.Application.Features.Administration.Users.GetUsers;
using OrderHub.Infrastructure.Features.Administration.Users;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.ReadServices;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Stores;
using System.Reflection;

namespace OrderHub.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddPersistence(connectionString);
        services.AddStores();
        services.AddRepositories();
        services.AddReadServices();
        services.AddDrafts();

        services.AddServices();

        services.AddTransient<UserReadService>();

        services.AddTransient<IUserListingService>(sp => sp.GetRequiredService<UserReadService>());

        services.AddTransient<IUserDetailsService>(provider => provider.GetRequiredService<UserReadService>());

        return services;
    }
}
