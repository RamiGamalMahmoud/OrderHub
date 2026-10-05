using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;

namespace OrderHub.Web.Services;

public static class EditorServiceRegistration
{
    public static IServiceCollection AddEditorServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticatedUser>();
        services.AddScoped<OperationUser>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<OperationUser>());
        services.AddScoped<IRequestExecutor, AuthenticatedRequestExecutor>();
        return services;
    }
}
