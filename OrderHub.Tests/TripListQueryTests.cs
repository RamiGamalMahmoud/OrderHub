using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Features.Trips.GetTripsPage;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;
using OrderHub.Infrastructure.Features.Trips;

namespace OrderHub.Tests;

public sealed class TripListQueryTests
{
    [Fact]
    public async Task List_projects_header_state_and_editability()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1, title: "Airport Run");

        var page = await Reader(db).GetPageAsync(new TripSearchCriteria(), 1, 25, CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal(trip.Id, item.Id);
        Assert.Equal(1L, item.TripNumber);
        Assert.Equal(TripTestDb.SequenceYear, item.SequenceYear);
        Assert.Equal(TripTestDb.SequenceMonth, item.SequenceMonth);
        Assert.Equal("Airport Run", item.Title);
        Assert.Equal(db.Deliveryman.Id, item.DeliverymanId);
        Assert.Equal("سالم", item.DeliverymanName);
        Assert.Equal(TripPartyType.External, item.FromType);
        Assert.Equal("المرسل", item.FromName);
        Assert.Equal("المستلم", item.ToName);
        Assert.Equal(WorkRecordStatus.Pending, item.Status);
        Assert.Equal(TripDisplayState.New, item.DisplayState);
        Assert.Null(item.CurrentAttemptNumber);
        Assert.Equal(0, item.AttemptCount);
        Assert.False(item.HasFailedAttempt);
        Assert.Equal(TripTestDb.AtUtc, item.CreatedAt);
        Assert.Equal(1, item.Version);
        Assert.True(item.IsEditable);
    }

    [Fact]
    public async Task List_orders_newest_first()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, title: "older", createdAtUtc: TripTestDb.AtUtc);
        db.PersistTrip(2, title: "middle", createdAtUtc: TripTestDb.AtUtc.AddMinutes(5));
        db.PersistTrip(3, title: "newest", createdAtUtc: TripTestDb.AtUtc.AddMinutes(10));

        var page = await Reader(db).GetPageAsync(new TripSearchCriteria(), 1, 25, CancellationToken.None);

        Assert.Equal(
            new[] { "newest", "middle", "older" },
            page.Items.Select(item => item.Title));
    }

    [Fact]
    public async Task Search_is_case_insensitive_and_matches_numbers()
    {
        using var db = new TripTestDb();
        db.PersistTrip(12345, title: "Airport Run");
        var reader = Reader(db);

        Assert.Single((await reader.GetPageAsync(
            new TripSearchCriteria("airport"), 1, 25, CancellationToken.None)).Items);
        Assert.Single((await reader.GetPageAsync(
            new TripSearchCriteria("AIRPORT"), 1, 25, CancellationToken.None)).Items);
        Assert.Single((await reader.GetPageAsync(
            new TripSearchCriteria("#12345"), 1, 25, CancellationToken.None)).Items);
        Assert.Empty((await reader.GetPageAsync(
            new TripSearchCriteria("123"), 1, 25, CancellationToken.None)).Items);
        Assert.Empty((await reader.GetPageAsync(
            new TripSearchCriteria("999999999999999999999999999999"), 1, 25, CancellationToken.None)).Items);
    }

    [Fact]
    public async Task Search_normalizes_phone_separators()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, to: TripTestDb.ExternalParty("المستلم", "0501112233"));
        db.PersistTrip(2, to: TripTestDb.ExternalParty("آخر", "0509999999"));

        var page = await Reader(db).GetPageAsync(
            new TripSearchCriteria("050 111 2233"), 1, 25, CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal(1L, item.TripNumber);
    }

    [Fact]
    public async Task Display_state_filters_follow_header_and_attempts()
    {
        using var db = new TripTestDb();
        var newTrip = db.PersistTrip(1, title: "new");
        var inProgress = db.PersistTrip(2, title: "progress");
        inProgress.Start(db.Operation());
        var failedHeader = db.PersistTrip(3, title: "failed");
        failedHeader.Start(db.Operation());
        failedHeader.Fail("رفض", db.Operation());
        var retried = db.PersistTrip(4, title: "retried");
        retried.Start(db.Operation());
        retried.Fail("فشل", db.Operation());
        retried.Retry(db.Operation());
        var completed = db.PersistTrip(5, title: "completed");
        completed.Start(db.Operation());
        completed.Complete(db.Operation());
        var cancelled = db.PersistTrip(6, title: "cancelled");
        cancelled.Cancel("إلغاء", db.Operation());
        db.Context.SaveChanges();

        var reader = Reader(db);

        async Task<int[]> IdsFor(params TripDisplayState[] states)
        {
            var page = await reader.GetPageAsync(
                new TripSearchCriteria(DisplayStates: states), 1, 25, CancellationToken.None);
            return page.Items.Select(item => item.Id).OrderBy(id => id).ToArray();
        }

        Assert.Equal(new[] { newTrip.Id }, await IdsFor(TripDisplayState.New));
        Assert.Equal(new[] { inProgress.Id }, await IdsFor(TripDisplayState.InProgress));
        Assert.Equal(new[] { retried.Id }, await IdsFor(TripDisplayState.AwaitingRetryStart));
        Assert.Equal(new[] { failedHeader.Id, retried.Id }, await IdsFor(TripDisplayState.Failed));
        Assert.Equal(new[] { completed.Id }, await IdsFor(TripDisplayState.Completed));
        Assert.Equal(new[] { cancelled.Id }, await IdsFor(TripDisplayState.Cancelled));
    }

    [Fact]
    public async Task Created_range_uses_riyadh_day_boundaries()
    {
        using var db = new TripTestDb();
        var early = db.PersistTrip(1, createdAtUtc: new DateTime(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc));
        var late = db.PersistTrip(2, createdAtUtc: new DateTime(2026, 11, 9, 21, 30, 0, DateTimeKind.Utc));
        var reader = Reader(db);

        var fromPage = await reader.GetPageAsync(
            new TripSearchCriteria(CreatedFrom: new DateOnly(2026, 11, 10)),
            1,
            25,
            CancellationToken.None);
        Assert.Equal(new[] { late.Id }, fromPage.Items.Select(item => item.Id));

        var toPage = await reader.GetPageAsync(
            new TripSearchCriteria(CreatedTo: new DateOnly(2026, 11, 9)),
            1,
            25,
            CancellationToken.None);
        Assert.Equal(new[] { early.Id }, toPage.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Scheduled_range_uses_half_open_utc_bounds_and_skips_unscheduled()
    {
        using var db = new TripTestDb();
        var scheduled = db.PersistTrip(
            1,
            scheduledAtUtc: new DateTimeOffset(2026, 11, 10, 6, 0, 0, TimeSpan.Zero));
        db.PersistTrip(2);
        var reader = Reader(db);

        var fromPage = await reader.GetPageAsync(
            new TripSearchCriteria(ScheduledFrom: new DateOnly(2026, 11, 10)),
            1,
            25,
            CancellationToken.None);
        Assert.Equal(new[] { scheduled.Id }, fromPage.Items.Select(item => item.Id));

        var toPage = await reader.GetPageAsync(
            new TripSearchCriteria(ScheduledTo: new DateOnly(2026, 11, 9)),
            1,
            25,
            CancellationToken.None);
        Assert.Empty(toPage.Items);
    }

    [Fact]
    public async Task Paging_inputs_are_clamped()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1, title: "one");
        db.PersistTrip(2, title: "two");
        db.PersistTrip(3, title: "three");
        var reader = Reader(db);

        var clamped = await reader.GetPageAsync(new TripSearchCriteria(), 0, 7, CancellationToken.None);
        Assert.Equal(1, clamped.PageNumber);
        Assert.Equal(25, clamped.PageSize);
        Assert.Equal(3, clamped.TotalCount);

        var allowed = await reader.GetPageAsync(new TripSearchCriteria(), 2, 50, CancellationToken.None);
        Assert.Equal(50, allowed.PageSize);
        Assert.Empty(allowed.Items);
    }

    [Fact]
    public async Task Deliveryman_and_party_type_filters_are_exact()
    {
        using var db = new TripTestDb();
        var clientTrip = db.PersistTrip(
            1,
            from: TripParty.FromClient(db.Client),
            to: TripParty.FromSupplier(db.Supplier));
        var otherTrip = db.PersistTrip(2, deliveryman: db.OtherDeliveryman);
        var reader = Reader(db);

        var byDeliveryman = await reader.GetPageAsync(
            new TripSearchCriteria(DeliverymanId: db.Deliveryman.Id), 1, 25, CancellationToken.None);
        Assert.Equal(new[] { clientTrip.Id }, byDeliveryman.Items.Select(item => item.Id));

        var byClient = await reader.GetPageAsync(
            new TripSearchCriteria(PartyType: TripPartyType.Client), 1, 25, CancellationToken.None);
        Assert.Equal(new[] { clientTrip.Id }, byClient.Items.Select(item => item.Id));

        var byExternal = await reader.GetPageAsync(
            new TripSearchCriteria(PartyType: TripPartyType.External), 1, 25, CancellationToken.None);
        Assert.Equal(new[] { otherTrip.Id }, byExternal.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task List_includes_order_bound_trips_with_order_metadata()
    {
        using var db = new TripTestDb();
        var standalone = db.PersistTrip(1, title: "standalone");
        var orderTrip = db.AddDirectOrderTrip(db.Deliveryman, 2, "ORD-1001");
        orderTrip.UpdateCreationDate(TripTestDb.AtUtc.AddMinutes(1));
        db.Context.SaveChanges();

        var page = await Reader(db).GetPageAsync(new TripSearchCriteria(), 1, 25, CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        Assert.Contains(page.Items, item => item.Source == TripSource.Standalone && item.Id == standalone.Id);

        var row = Assert.Single(page.Items, item => item.Source == TripSource.OrderBound);
        Assert.Equal(orderTrip.Id, row.Id);
        Assert.Equal(2L, row.TripNumber);
        Assert.Null(row.Title);
        Assert.Null(row.FromType);
        Assert.Null(row.FromName);
        Assert.Null(row.ToType);
        Assert.Null(row.ToName);
        Assert.Null(row.ScheduledAtUtc);
        Assert.Equal(db.Deliveryman.Id, row.DeliverymanId);
        Assert.Equal("سالم", row.DeliverymanName);
        Assert.Equal(WorkRecordStatus.Pending, row.Status);
        Assert.Equal(TripDisplayState.InProgress, row.DisplayState);
        Assert.Equal(0, row.AttemptCount);
        Assert.Null(row.CurrentAttemptNumber);
        Assert.False(row.HasFailedAttempt);
        Assert.False(row.IsEditable);
        Assert.NotNull(row.OrderId);
        Assert.Equal("ORD-1001", row.OrderNumber);
    }

    [Fact]
    public async Task List_resolves_order_for_delivery_step_trips()
    {
        using var db = new TripTestDb();
        var stepTrip = db.AddStepOrderTrip(db.OtherDeliveryman, 7, "ORD-STEP-7");

        var page = await Reader(db).GetPageAsync(new TripSearchCriteria(), 1, 25, CancellationToken.None);

        var row = Assert.Single(page.Items);
        Assert.Equal(TripSource.OrderBound, row.Source);
        Assert.Equal(stepTrip.Id, row.Id);
        Assert.Equal(db.OtherDeliveryman.Id, row.DeliverymanId);
        Assert.Equal("ناصر", row.DeliverymanName);
        Assert.NotNull(row.OrderId);
        Assert.Equal("ORD-STEP-7", row.OrderNumber);
    }

    [Fact]
    public async Task List_interleaves_sources_by_created_at_and_pages_stably()
    {
        using var db = new TripTestDb();
        var expected = new List<(TripSource Source, long Number)>();

        for (int minute = 1; minute <= 11; minute++)
        {
            if (minute % 2 == 1)
            {
                db.PersistTrip(minute, title: $"standalone-{minute}", createdAtUtc: TripTestDb.AtUtc.AddMinutes(minute));
                expected.Add((TripSource.Standalone, minute));
            }
            else
            {
                var orderTrip = db.AddDirectOrderTrip(db.Deliveryman, minute, $"ORD-{minute}");
                orderTrip.UpdateCreationDate(TripTestDb.AtUtc.AddMinutes(minute));
                expected.Add((TripSource.OrderBound, minute));
            }
        }

        db.Context.SaveChanges();
        expected.Reverse();

        var reader = Reader(db);
        var first = await reader.GetPageAsync(new TripSearchCriteria(), 1, 10, CancellationToken.None);
        var second = await reader.GetPageAsync(new TripSearchCriteria(), 2, 10, CancellationToken.None);

        Assert.Equal(11, first.TotalCount);
        Assert.Equal(10, first.Items.Count());
        Assert.Single(second.Items);

        var actual = first.Items
            .Concat(second.Items)
            .Select(item => (item.Source, item.TripNumber))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Search_matches_order_number_and_bound_trip_number()
    {
        using var db = new TripTestDb();
        db.AddDirectOrderTrip(db.Deliveryman, 500, "ORD-ALPHA");
        db.PersistTrip(1, title: "standalone");

        var reader = Reader(db);

        var byOrderNumber = await reader.GetPageAsync(
            new TripSearchCriteria("ord-alpha"), 1, 25, CancellationToken.None);
        Assert.Equal(TripSource.OrderBound, Assert.Single(byOrderNumber.Items).Source);

        var byTripNumber = await reader.GetPageAsync(
            new TripSearchCriteria("#500"), 1, 25, CancellationToken.None);
        Assert.Equal(TripSource.OrderBound, Assert.Single(byTripNumber.Items).Source);
    }

    [Fact]
    public async Task Party_and_schedule_filters_exclude_order_bound_rows()
    {
        using var db = new TripTestDb();
        var standalone = db.PersistTrip(1);
        db.AddDirectOrderTrip(db.Deliveryman, 2);

        var reader = Reader(db);

        var byParty = await reader.GetPageAsync(
            new TripSearchCriteria(PartyType: TripPartyType.External), 1, 25, CancellationToken.None);
        Assert.Equal(new[] { standalone.Id }, byParty.Items.Select(item => item.Id));

        var bySchedule = await reader.GetPageAsync(
            new TripSearchCriteria(ScheduledFrom: new DateOnly(2026, 1, 1)), 1, 25, CancellationToken.None);
        Assert.Empty(bySchedule.Items);
    }

    [Fact]
    public async Task Display_state_filter_maps_order_bound_pending_to_in_progress()
    {
        using var db = new TripTestDb();
        var pending = db.AddDirectOrderTrip(db.Deliveryman, 2);
        var completed = db.AddDirectOrderTrip(db.Deliveryman, 3, "ORD-DONE");
        completed.TransitionTo(WorkRecordStatus.Completed);
        db.Context.SaveChanges();

        var reader = Reader(db);

        var inProgress = await reader.GetPageAsync(
            new TripSearchCriteria(DisplayStates: [TripDisplayState.InProgress]),
            1,
            25,
            CancellationToken.None);
        Assert.Equal(new[] { pending.Id }, inProgress.Items.Select(item => item.Id));

        var done = await reader.GetPageAsync(
            new TripSearchCriteria(DisplayStates: [TripDisplayState.Completed]),
            1,
            25,
            CancellationToken.None);
        var doneRow = Assert.Single(done.Items);
        Assert.Equal(completed.Id, doneRow.Id);
        Assert.Equal(TripDisplayState.Completed, doneRow.DisplayState);
    }

    [Fact]
    public void List_query_requires_trips_read_permission()
    {
        var attribute = typeof(GetTripsPageQuery).GetCustomAttribute<RequirePermissionAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(PermissionFeature.Trips, attribute!.Feature);
        Assert.Equal(PermissionLevel.Read, attribute.RequiredLevel);
    }

    private static TripReader Reader(TripTestDb db) =>
        new(db.Factory, new StandaloneTripRepository(db.Context));
}
