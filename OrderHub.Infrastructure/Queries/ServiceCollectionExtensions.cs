using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Orders.NotifyOrderParticipants;
using OrderHub.Infrastructure.Queries.Orders;

namespace OrderHub.Infrastructure.Queries;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddScoped<OrderQueries>();

        services.AddScoped<IOrderNotificationQuery>(
            sp => sp.GetRequiredService<OrderQueries>());

        return services;
    }
}
