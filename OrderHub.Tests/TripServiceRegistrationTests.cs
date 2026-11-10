using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Trips;

namespace OrderHub.Tests;

public sealed class TripServiceRegistrationTests
{
    [Fact]
    public void AddTripsFeature_registers_repositories_and_readers_as_scoped()
    {
        using var db = new TripTestDb();
        var services = new ServiceCollection();
        services.AddSingleton(db.Context);
        services.AddSingleton(db.Factory);

        Assert.Same(services, services.AddTripsFeature());

        AssertRegistration<IStandaloneTripRepository, StandaloneTripRepository>(services);
        AssertRegistration<ITripReader, TripReader>(services);
        AssertRegistration<ITripUsageReader, TripUsageReader>(services);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<StandaloneTripRepository>(
            scope.ServiceProvider.GetRequiredService<IStandaloneTripRepository>());
        Assert.IsType<TripReader>(scope.ServiceProvider.GetRequiredService<ITripReader>());
        Assert.IsType<TripUsageReader>(scope.ServiceProvider.GetRequiredService<ITripUsageReader>());
    }

    [Fact]
    public void AddTripsFeature_is_safe_to_call_twice()
    {
        using var db = new TripTestDb();
        var services = new ServiceCollection();
        services.AddSingleton(db.Context);
        services.AddSingleton(db.Factory);

        services.AddTripsFeature();
        services.AddTripsFeature();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripReader>());
    }

    private static void AssertRegistration<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        var descriptor = Assert.Single(services, item => item.ServiceType == typeof(TService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
    }
}
