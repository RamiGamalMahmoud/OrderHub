using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Features.Trips;

namespace OrderHub.Tests;

public sealed class StandaloneTripRepositoryTests
{
    [Fact]
    public async Task GetById_returns_attempts_and_null_for_unknown_trips()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        trip.Start(db.Operation());
        db.Context.SaveChanges();
        var repository = new StandaloneTripRepository(db.Context);

        db.Context.ChangeTracker.Clear();
        var loaded = await repository.GetByIdAsync(trip.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Version);
        Assert.Single(loaded.Attempts);
        Assert.Empty(loaded.History);

        Assert.Null(await repository.GetByIdAsync(4242, CancellationToken.None));
    }

    [Fact]
    public async Task GetForUpdate_loads_history_and_GetForEdit_loads_the_deliveryman()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        var repository = new StandaloneTripRepository(db.Context);

        db.Context.ChangeTracker.Clear();
        var forUpdate = await repository.GetForUpdateAsync(trip.Id, CancellationToken.None);
        Assert.NotNull(forUpdate);
        Assert.Single(forUpdate!.History);
        Assert.Equal(StandaloneTripEventType.Created, forUpdate.History.Single().EventType);

        db.Context.ChangeTracker.Clear();
        var forEdit = await repository.GetForEditAsync(trip.Id, CancellationToken.None);
        Assert.NotNull(forEdit);
        Assert.NotNull(forEdit!.Deliveryman);
        Assert.Equal("سالم", forEdit.Deliveryman.Name.Value);
    }

    [Fact]
    public async Task Order_bound_page_resolves_direct_and_step_bound_orders()
    {
        using var db = new TripTestDb();
        var direct = db.AddDirectOrderTrip(db.Deliveryman, 1, "ORD-TRIP-1");
        var step = db.AddStepOrderTrip(db.OtherDeliveryman, 2, "ORD-TRIP-STEP");
        var repository = new StandaloneTripRepository(db.Context);

        var directPage = await repository.GetOrderBoundTripsPageAsync(
            db.Deliveryman.Id, 1, 10, CancellationToken.None);
        var directItem = Assert.Single(directPage.Items);
        Assert.Equal(direct.Id, directItem.DeliveryTripId);
        Assert.Equal(1L, directItem.WorkRecordNumber);
        Assert.NotNull(directItem.OrderId);
        Assert.Equal("ORD-TRIP-1", directItem.OrderNumber);
        Assert.Equal(OrderStatus.OutForDelivery, directItem.OrderStatus);

        var stepPage = await repository.GetOrderBoundTripsPageAsync(
            db.OtherDeliveryman.Id, 1, 10, CancellationToken.None);
        var stepItem = Assert.Single(stepPage.Items);
        Assert.Equal(step.Id, stepItem.DeliveryTripId);
        Assert.NotNull(stepItem.OrderId);
        Assert.Equal("ORD-TRIP-STEP", stepItem.OrderNumber);
        Assert.Equal(OrderStatus.OutForDelivery, stepItem.OrderStatus);

        Assert.Equal(1, await repository.CountOrderBoundTripsAsync(db.Deliveryman.Id, CancellationToken.None));
        Assert.Equal(1, await repository.CountOrderBoundTripsAsync(db.OtherDeliveryman.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Order_bound_page_clamps_paging_and_orders_newest_first()
    {
        using var db = new TripTestDb();
        var createdIds = new System.Collections.Generic.List<int>();
        for (int index = 0; index < 12; index++)
        {
            createdIds.Add(db.AddDirectOrderTrip(db.Deliveryman, 100 + index, $"ORD-PAGE-{index}").Id);
        }

        var repository = new StandaloneTripRepository(db.Context);
        var firstPage = await repository.GetOrderBoundTripsPageAsync(
            db.Deliveryman.Id, 0, 7, CancellationToken.None);

        Assert.Equal(1, firstPage.PageNumber);
        Assert.Equal(10, firstPage.PageSize);
        Assert.Equal(12, firstPage.TotalCount);
        Assert.Equal(10, firstPage.Items.Count());
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(createdIds[^1], firstPage.Items.First().DeliveryTripId);

        var secondPage = await repository.GetOrderBoundTripsPageAsync(
            db.Deliveryman.Id, 2, 10, CancellationToken.None);
        Assert.Equal(2, secondPage.Items.Count());
    }
}
