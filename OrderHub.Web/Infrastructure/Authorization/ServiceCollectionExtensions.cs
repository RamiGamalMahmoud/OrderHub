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
        services.AddScoped<IAuthorizationHandler, HigherManagerRequirementHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var feature in Enum.GetValues<PermissionFeature>())
            foreach (var level in Enum.GetValues<PermissionLevel>())
            {
                if (level == PermissionLevel.None)
                    continue;

                options.AddPolicy(PermissionAuthorizeAttribute.PolicyName(feature, level), policy =>
                    policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(feature, level)));
            }

            options.AddPolicy(HigherManagerAuthorizeAttribute.PolicyName, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new HigherManagerRequirement()));
        });
        return services;
    }
}
