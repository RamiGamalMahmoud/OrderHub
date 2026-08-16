using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.OrderDrafts.Contracts;

namespace OrderHub.Infrastructure.Features.OrderDrafts;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDrafts(this IServiceCollection services)
    {
        services.AddSingleton<IDraftService, DraftService>();
        services.AddSingleton<IDraftStore, JsonDraftStore>();
        return services;
    }
}
