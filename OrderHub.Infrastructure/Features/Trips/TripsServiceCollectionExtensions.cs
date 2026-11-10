using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Interfaces;

namespace OrderHub.Infrastructure.Features.Trips;

internal static class TripsServiceCollectionExtensions
{
    public static IServiceCollection AddTripsFeature(this IServiceCollection services)
    {
        services.AddScoped<IStandaloneTripRepository, StandaloneTripRepository>();
        services.AddScoped<ITripReader, TripReader>();
        services.AddScoped<ITripUsageReader, TripUsageReader>();
        return services;
    }
}
