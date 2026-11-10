using System.Threading;
using System.Threading.Tasks;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;
using OrderHub.Infrastructure.Features.Trips;

namespace OrderHub.Tests;

public sealed class TripUsageReaderTests
{
    [Fact]
    public async Task Party_references_are_detected_on_both_ends()
    {
        using var db = new TripTestDb();
        db.PersistTrip(
            1,
            from: TripParty.FromClient(db.Client),
            to: TripParty.FromSupplier(db.Supplier));
        var reader = new TripUsageReader(db.Context);

        Assert.True(await reader.HasPartyTripsAsync(TripPartyType.Client, db.Client.Id, CancellationToken.None));
        Assert.True(await reader.HasPartyTripsAsync(TripPartyType.Supplier, db.Supplier.Id, CancellationToken.None));
        Assert.False(await reader.HasPartyTripsAsync(TripPartyType.Client, db.SecondClient.Id, CancellationToken.None));
        Assert.False(await reader.HasPartyTripsAsync(
            TripPartyType.ShippingCarrier, db.Carrier.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Deliveryman_references_include_current_and_historical_assignments()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        var reader = new TripUsageReader(db.Context);

        Assert.True(await reader.HasDeliverymanTripsAsync(db.Deliveryman.Id, CancellationToken.None));
        Assert.False(await reader.HasDeliverymanTripsAsync(db.OtherDeliveryman.Id, CancellationToken.None));

        var loaded = db.ReloadWithDeliveryman(trip.Id);
        loaded.Update(
            loaded.Title,
            loaded.Description,
            loaded.From,
            loaded.To,
            loaded.ScheduledAtUtc,
            db.OtherDeliveryman.Id,
            "ناصر",
            99,
            "تغيير المندوب",
            db.Operation());
        db.Context.SaveChanges();

        Assert.True(await reader.HasDeliverymanTripsAsync(db.Deliveryman.Id, CancellationToken.None));
        Assert.True(await reader.HasDeliverymanTripsAsync(db.OtherDeliveryman.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Unrelated_master_data_is_reported_as_unused()
    {
        using var db = new TripTestDb();
        var reader = new TripUsageReader(db.Context);

        Assert.False(await reader.HasDeliverymanTripsAsync(db.Deliveryman.Id, CancellationToken.None));
        Assert.False(await reader.HasPartyTripsAsync(TripPartyType.Client, db.Client.Id, CancellationToken.None));
        Assert.False(await reader.HasPartyTripsAsync(TripPartyType.Supplier, db.Supplier.Id, CancellationToken.None));
        Assert.False(await reader.HasPartyTripsAsync(
            TripPartyType.ShippingCarrier, db.Carrier.Id, CancellationToken.None));
    }
}
