using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace OrderHub.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<AppDbContextFactory>();
        services.AddSingleton<Application.Interfaces.Services.IDatabaseService, Services.DatabaseService>();
        services.AddHttpClient<Application.Interfaces.Services.IAuthService>();
        services.AddSingleton<Application.Interfaces.Services.IOrderService, Services.OrderService>();
        services.AddSingleton<Application.Interfaces.Services.IApplicationDirectoriesService, Services.ApplicationDirectoriesService>();
        services.AddSingleton<Application.Interfaces.Services.IEncryptionService, Services.EncryptionService>();
        services.AddSingleton<Application.Interfaces.Services.ICredentialsService, Services.FileCredentialsService>();
        services.AddSingleton<Application.Interfaces.Services.ITokenStorageService, Services.FieTokenStorageService>();
        services.AddTransient<Application.Interfaces.Services.IAuthService, Services.SalaAuthService>();
        services.AddSingleton<Application.Interfaces.Services.ICacheService, Services.CacheService>();
        return services;
    }
}
