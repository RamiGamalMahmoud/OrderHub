using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Infrastructure.Features.OrderDrafts;

namespace OrderHub.Web.Services;

public static class EditorServiceRegistration
{
    public static IServiceCollection AddEditorServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticatedUser>();
        services.AddScoped<OperationUser>();
        services.AddScoped<ICurrentUser>(provider =>
        {
            var operation = provider.GetRequiredService<OperationUser>();
            return operation.IsInitialized ? operation : provider.GetRequiredService<AuthenticatedUser>();
        });
        services.AddScoped<IRequestExecutor, AuthenticatedRequestExecutor>();
        services.AddScoped<IDraftService, DraftService>();
        return services;
    }
}
