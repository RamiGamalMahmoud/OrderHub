using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Features.Trips;

namespace OrderHub.Tests;

public sealed class DeliverymanTripsQueryTests
{
    [Fact]
    public async Task Summary_counts_open_failed_completed_cancelled_and_total()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, title: "جديد");
        var inProgress = db.PersistTrip(2, title: "جارٍ");
        inProgress.Start(db.Operation());
        var failed = db.PersistTrip(3, title: "فاشل");
        failed.Start(db.Operation());
        failed.Fail("فشل", db.Operation());
        var retried = db.PersistTrip(4, title: "بانتظار الإعادة");
        retried.Start(db.Operation());
        retried.Fail("فشل", db.Operation());
        retried.Retry(db.Operation());
        var completed = db.PersistTrip(5, title: "مكتمل");
        completed.Start(db.Operation());
        completed.Complete(db.Operation());
        var cancelled = db.PersistTrip(6, title: "ملغى");
        cancelled.Cancel("إلغاء", db.Operation());
        db.PersistTrip(7, deliveryman: db.OtherDeliveryman, title: "لمندوب آخر");
        db.Context.SaveChanges();

        var summary = await Reader(db).GetDeliverymanSummaryAsync(db.Deliveryman.Id, CancellationToken.None);

        Assert.Equal(3, summary.OpenCount);
        Assert.Equal(2, summary.FailedCount);
        Assert.Equal(1, summary.CompletedCount);
        Assert.Equal(1, summary.CancelledCount);
        Assert.Equal(6, summary.TotalCount);
    }

    [Fact]
    public async Task Summary_ignores_order_bound_trips()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1);
        var reader = Reader(db);
        var before = await reader.GetDeliverymanSummaryAsync(db.Deliveryman.Id, CancellationToken.None);

        db.AddDirectOrderTrip(db.Deliveryman, 200, "ORD-SUMMARY-1");
        db.AddStepOrderTrip(db.Deliveryman, 201, "ORD-SUMMARY-2");
        var after = await reader.GetDeliverymanSummaryAsync(db.Deliveryman.Id, CancellationToken.None);

        Assert.Equal(before, after);
        Assert.Equal(1, after.TotalCount);
    }

    [Fact]
    public async Task Standalone_page_filters_deliveryman_orders_newest_first_and_clamps()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, title: "older", createdAtUtc: TripTestDb.AtUtc);
        db.PersistTrip(2, title: "middle", createdAtUtc: TripTestDb.AtUtc.AddMinutes(5));
        db.PersistTrip(3, title: "newest", createdAtUtc: TripTestDb.AtUtc.AddMinutes(10));
        db.PersistTrip(4, deliveryman: db.OtherDeliveryman, title: "other");

        var page = await Reader(db).GetDeliverymanStandaloneTripsPageAsync(
            db.Deliveryman.Id, 0, 7, CancellationToken.None);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(
            new[] { "newest", "middle", "older" },
            page.Items.Select(item => item.Title));
        Assert.All(page.Items, item => Assert.True(item.IsEditable));
        Assert.All(page.Items, item => Assert.Equal(TripDisplayState.New, item.DisplayState));
    }

    [Fact]
    public async Task Order_bound_page_uses_numbering_party_and_resolves_both_order_shapes()
    {
        using var db = new TripTestDb();
        db.AddDirectOrderTrip(db.Deliveryman, 1, "ORD-DIRECT");
        db.AddStepOrderTrip(db.OtherDeliveryman, 2, "ORD-STEP");
        var reader = Reader(db);

        var directPage = await reader.GetDeliverymanOrderBoundTripsPageAsync(
            db.Deliveryman.Id, 1, 10, CancellationToken.None);
        var direct = Assert.Single(directPage.Items);
        Assert.NotNull(direct.OrderId);
        Assert.Equal("ORD-DIRECT", direct.OrderNumber);
        Assert.Equal(OrderStatus.OutForDelivery, direct.OrderStatus);

        var stepPage = await reader.GetDeliverymanOrderBoundTripsPageAsync(
            db.OtherDeliveryman.Id, 1, 10, CancellationToken.None);
        var step = Assert.Single(stepPage.Items);
        Assert.NotNull(step.OrderId);
        Assert.Equal("ORD-STEP", step.OrderNumber);
        Assert.Equal(OrderStatus.OutForDelivery, step.OrderStatus);

        var missing = await reader.GetDeliverymanOrderBoundTripsPageAsync(
            db.Deliveryman.Id + db.OtherDeliveryman.Id + 1000, 1, 10, CancellationToken.None);
        Assert.Empty(missing.Items);
        Assert.Equal(0, missing.TotalCount);
    }

    [Fact]
    public async Task Order_bound_page_clamps_page_size()
    {
        using var db = new TripTestDb();
        for (int index = 0; index < 12; index++)
        {
            db.AddDirectOrderTrip(db.Deliveryman, 300 + index, $"ORD-CLAMP-{index}");
        }

        var page = await Reader(db).GetDeliverymanOrderBoundTripsPageAsync(
            db.Deliveryman.Id, 0, 7, CancellationToken.None);

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(12, page.TotalCount);
        Assert.Equal(10, page.Items.Count());
    }

    private static TripReader Reader(TripTestDb db) =>
        new(db.Factory, new StandaloneTripRepository(db.Context));
}
