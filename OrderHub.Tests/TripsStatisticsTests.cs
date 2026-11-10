using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Trips.CancelTrip;
using OrderHub.Application.Features.Trips.CompleteTrip;
using OrderHub.Application.Features.Trips.FailTrip;
using OrderHub.Application.Features.Trips.GetTripsPage;
using OrderHub.Application.Features.Trips.GetTripsStatistics;
using OrderHub.Application.Features.Trips.StartTrip;
using OrderHub.Infrastructure.Features.Trips;

namespace OrderHub.Tests;

/// <summary>
/// Trips statistics reader tests: the <c>/trips</c> KPI cards must count both
/// standalone and order-bound trips, grouped by WorkRecord status.
/// </summary>
public class TripsStatisticsTests
{
    [Fact]
    public async Task Statistics_count_active_completed_and_order_bound_trips()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var pending = db.PersistTrip(1);
        var completed = db.PersistTrip(2);
        Assert.True((await ctx.StartHandler().Handle(new StartTripCommand(completed.Id, 1), CancellationToken.None)).IsSuccess);
        Assert.True((await ctx.CompleteHandler().Handle(new CompleteTripCommand(completed.Id, 2), CancellationToken.None)).IsSuccess);

        db.AddDirectOrderTrip(db.Deliveryman, 1);

        var reader = new TripReader(db.Factory, ctx.Repository);
        var stats = await reader.GetStatisticsAsync(CancellationToken.None);
        var handlerResult = await new GetTripsStatisticsQueryHandler(reader)
            .Handle(new GetTripsStatisticsQuery(), CancellationToken.None);

        Assert.Equal(3, stats.TotalCount);
        Assert.Equal(2, stats.ActiveCount);
        Assert.Equal(1, stats.CompletedCount);
        Assert.Equal(0, stats.FailedCount);
        Assert.Equal(0, stats.CancelledCount);
        Assert.Equal(stats, handlerResult);
    }

    [Fact]
    public async Task Statistics_count_failed_and_cancelled_trips()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var failed = db.PersistTrip(1);
        Assert.True((await ctx.StartHandler().Handle(new StartTripCommand(failed.Id, 1), CancellationToken.None)).IsSuccess);
        Assert.True((await ctx.FailHandler().Handle(new FailTripCommand(failed.Id, 2, "العميل لا يستجيب"), CancellationToken.None)).IsSuccess);

        var cancelled = db.PersistTrip(2);
        Assert.True((await ctx.CancelHandler().Handle(new CancelTripCommand(cancelled.Id, 1, "تغيير موعد التسليم"), CancellationToken.None)).IsSuccess);

        var reader = new TripReader(db.Factory, ctx.Repository);
        var stats = await reader.GetStatisticsAsync(CancellationToken.None);

        Assert.Equal(2, stats.TotalCount);
        Assert.Equal(0, stats.ActiveCount);
        Assert.Equal(0, stats.CompletedCount);
        Assert.Equal(1, stats.FailedCount);
        Assert.Equal(1, stats.CancelledCount);
    }

    [Fact]
    public async Task Statistics_are_empty_for_an_empty_database()
    {
        using var db = new TripTestDb();
        var reader = new TripReader(db.Factory, new StandaloneTripRepository(db.Context));

        var stats = await reader.GetStatisticsAsync(CancellationToken.None);

        Assert.Equal(0, stats.TotalCount);
        Assert.Equal(0, stats.ActiveCount);
        Assert.Equal(0, stats.CompletedCount);
        Assert.Equal(0, stats.FailedCount);
        Assert.Equal(0, stats.CancelledCount);
    }
}
