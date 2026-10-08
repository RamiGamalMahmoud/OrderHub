using Microsoft.AspNetCore.Authorization;
using OrderHub.Domain.Enums;
using OrderHub.Web.Services;

namespace OrderHub.Web.Infrastructure.Authorization;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<PermissionAccess>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PermissionAuthorizationResultHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var feature in Enum.GetValues<PermissionFeature>())
            foreach (var level in new[] { PermissionLevel.Read, PermissionLevel.Write })
                options.AddPolicy(PermissionAuthorizeAttribute.PolicyName(feature, level), policy =>
                    policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(feature, level)));
        });
        return services;
    }
}
