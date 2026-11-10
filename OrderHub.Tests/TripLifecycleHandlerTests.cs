using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Trips.CancelTrip;
using OrderHub.Application.Features.Trips.CompleteTrip;
using OrderHub.Application.Features.Trips.FailTrip;
using OrderHub.Application.Features.Trips.RetryTrip;
using OrderHub.Application.Features.Trips.StartTrip;
using OrderHub.Domain.Enums;

namespace OrderHub.Tests;

public sealed class TripLifecycleHandlerTests
{
    [Fact]
    public async Task Start_creates_the_first_attempt_and_increments_version_once()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(2, reloaded.Version);
        Assert.Equal(WorkRecordStatus.Pending, reloaded.Status);
        Assert.Equal(TripTestDb.AtUtc, reloaded.StartedAtUtc);
        Assert.Equal(TripDisplayState.InProgress, reloaded.GetDisplayState());

        var attempt = Assert.Single(reloaded.Attempts);
        Assert.Equal(1, attempt.AttemptNumber);
        Assert.Equal(WorkRecordStatus.Pending, attempt.Status);
        Assert.Equal(TripTestDb.AtUtc, attempt.StartedAtUtc);
        Assert.Null(attempt.EndedAtUtc);
        Assert.Contains(
            reloaded.History,
            entry => entry.EventType == StandaloneTripEventType.AttemptStarted);
        Assert.Equal(0, ctx.Sequence.Calls);
    }

    [Fact]
    public async Task Stale_version_returns_conflict_and_writes_nothing()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 9), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(1, reloaded.Version);
        Assert.Empty(reloaded.Attempts);
        Assert.Single(reloaded.History);
    }

    [Fact]
    public async Task Starting_an_already_started_trip_is_rejected()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        Assert.True((await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None)).IsSuccess);

        var result = await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 2), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Contains("لا يمكن بدء المشوار", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(2, reloaded.Version);
        Assert.Single(reloaded.Attempts);
    }

    [Fact]
    public async Task Complete_requires_a_started_open_attempt()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.CompleteHandler().Handle(new CompleteTripCommand(trip.Id, 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Contains("لا يوجد محاولة جارية", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(1, reloaded.Version);
        Assert.Equal(WorkRecordStatus.Pending, reloaded.Status);
    }

    [Fact]
    public async Task Complete_closes_the_attempt_and_the_header()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);

        var result = await ctx.CompleteHandler().Handle(new CompleteTripCommand(trip.Id, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(WorkRecordStatus.Completed, reloaded.Status);
        Assert.Equal(TripDisplayState.Completed, reloaded.GetDisplayState());
        var attempt = Assert.Single(reloaded.Attempts);
        Assert.Equal(WorkRecordStatus.Completed, attempt.Status);
        Assert.Equal(TripTestDb.AtUtc, attempt.EndedAtUtc);
        Assert.Contains(
            reloaded.History,
            entry => entry.EventType == StandaloneTripEventType.Completed);
    }

    [Fact]
    public async Task Fail_reason_is_validated_before_any_change()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);

        var blank = await ctx.FailHandler().Handle(
            new FailTripCommand(trip.Id, 2, "   "), CancellationToken.None);
        var tooLong = await ctx.FailHandler().Handle(
            new FailTripCommand(trip.Id, 2, new string('س', 201)), CancellationToken.None);

        Assert.False(blank.IsSuccess);
        Assert.False(tooLong.IsSuccess);
        Assert.Equal("InvalidOperation", blank.ErrorCode);
        Assert.Equal("InvalidOperation", tooLong.ErrorCode);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(2, reloaded.Version);
        Assert.Equal(WorkRecordStatus.Pending, reloaded.Status);
        Assert.DoesNotContain(
            reloaded.History,
            entry => entry.EventType == StandaloneTripEventType.Failed);
    }

    [Fact]
    public async Task Fail_records_the_reason_on_attempt_and_header()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);

        var result = await ctx.FailHandler().Handle(
            new FailTripCommand(trip.Id, 2, "رفض الاستلام"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(WorkRecordStatus.Failed, reloaded.Status);
        Assert.Equal(TripDisplayState.Failed, reloaded.GetDisplayState());
        var attempt = Assert.Single(reloaded.Attempts);
        Assert.Equal(WorkRecordStatus.Failed, attempt.Status);
        Assert.Equal("رفض الاستلام", attempt.Reason);
        Assert.NotNull(attempt.EndedAtUtc);
        Assert.Contains(
            reloaded.History,
            entry => entry.EventType == StandaloneTripEventType.Failed && entry.Reason == "رفض الاستلام");
    }

    [Fact]
    public async Task Retry_creates_an_unstarted_pending_attempt()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);
        await ctx.FailHandler().Handle(new FailTripCommand(trip.Id, 2, "تعذر الوصول"), CancellationToken.None);

        var result = await ctx.RetryHandler().Handle(new RetryTripCommand(trip.Id, 3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(WorkRecordStatus.Pending, reloaded.Status);
        Assert.Equal(TripDisplayState.AwaitingRetryStart, reloaded.GetDisplayState());
        Assert.Equal(2, reloaded.Attempts.Count);
        var retryAttempt = reloaded.Attempts.Single(attempt => attempt.AttemptNumber == 2);
        Assert.Equal(WorkRecordStatus.Pending, retryAttempt.Status);
        Assert.Null(retryAttempt.StartedAtUtc);
        Assert.Contains(
            reloaded.History,
            entry => entry.EventType == StandaloneTripEventType.Retried);
    }

    [Fact]
    public async Task Retry_before_failure_is_rejected()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.RetryHandler().Handle(new RetryTripCommand(trip.Id, 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Contains("مشوار فاشل فقط", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(1, db.Reload(trip.Id).Version);
    }

    [Fact]
    public async Task Cancel_from_a_new_trip_writes_no_attempt()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.CancelHandler().Handle(
            new CancelTripCommand(trip.Id, 1, "لم يعد مطلوباً"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(WorkRecordStatus.Cancelled, reloaded.Status);
        Assert.Equal(TripDisplayState.Cancelled, reloaded.GetDisplayState());
        Assert.Empty(reloaded.Attempts);
        var cancelled = reloaded.History.Single(
            entry => entry.EventType == StandaloneTripEventType.Cancelled);
        Assert.Null(cancelled.AttemptNumber);
        Assert.Equal("لم يعد مطلوباً", cancelled.Reason);
    }

    [Fact]
    public async Task Cancel_from_in_progress_closes_the_open_attempt()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);

        var result = await ctx.CancelHandler().Handle(
            new CancelTripCommand(trip.Id, 2, "إلغاء من العميل"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(WorkRecordStatus.Cancelled, reloaded.Status);
        var attempt = Assert.Single(reloaded.Attempts);
        Assert.Equal(WorkRecordStatus.Cancelled, attempt.Status);
        Assert.Equal("إلغاء من العميل", attempt.Reason);
        Assert.NotNull(attempt.EndedAtUtc);
    }

    [Fact]
    public async Task Cancel_from_failed_is_allowed()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);
        await ctx.FailHandler().Handle(new FailTripCommand(trip.Id, 2, "فشل"), CancellationToken.None);

        var result = await ctx.CancelHandler().Handle(
            new CancelTripCommand(trip.Id, 3, "إغلاق نهائي"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(WorkRecordStatus.Cancelled, reloaded.Status);
        Assert.Equal(WorkRecordStatus.Failed, Assert.Single(reloaded.Attempts).Status);
    }

    [Fact]
    public async Task Terminal_states_reject_every_transition()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);
        await ctx.CompleteHandler().Handle(new CompleteTripCommand(trip.Id, 2), CancellationToken.None);

        var start = await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 3), CancellationToken.None);
        var complete = await ctx.CompleteHandler().Handle(new CompleteTripCommand(trip.Id, 3), CancellationToken.None);
        var fail = await ctx.FailHandler().Handle(new FailTripCommand(trip.Id, 3, "سبب"), CancellationToken.None);
        var cancel = await ctx.CancelHandler().Handle(new CancelTripCommand(trip.Id, 3, "سبب"), CancellationToken.None);
        var retry = await ctx.RetryHandler().Handle(new RetryTripCommand(trip.Id, 3), CancellationToken.None);

        Assert.All(new[] { start, complete, fail, cancel, retry }, result =>
        {
            Assert.False(result.IsSuccess);
            Assert.Equal("InvalidOperation", result.ErrorCode);
        });

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(3, reloaded.Version);
        Assert.Equal(WorkRecordStatus.Completed, reloaded.Status);
    }

    [Fact]
    public async Task Cancelled_trip_rejects_start()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        await ctx.CancelHandler().Handle(new CancelTripCommand(trip.Id, 1, "إلغاء"), CancellationToken.None);

        var result = await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 2), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(2, db.Reload(trip.Id).Version);
    }

    [Fact]
    public async Task Unknown_trip_returns_not_found()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var result = await ctx.StartHandler().Handle(new StartTripCommand(4242, 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
    }

    [Fact]
    public async Task Unauthenticated_user_is_rejected()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.StartHandler(TestCurrentUser.Anonymous)
            .Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.ErrorCode);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(1, reloaded.Version);
        Assert.Empty(reloaded.Attempts);
    }

    [Fact]
    public async Task Full_lifecycle_consumes_no_sequence_numbers()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 1), CancellationToken.None);
        await ctx.FailHandler().Handle(new FailTripCommand(trip.Id, 2, "محاولة أولى"), CancellationToken.None);
        await ctx.RetryHandler().Handle(new RetryTripCommand(trip.Id, 3), CancellationToken.None);
        var started = await ctx.StartHandler().Handle(new StartTripCommand(trip.Id, 4), CancellationToken.None);
        var completed = await ctx.CompleteHandler().Handle(new CompleteTripCommand(trip.Id, 5), CancellationToken.None);

        Assert.True(started.IsSuccess);
        Assert.True(completed.IsSuccess);
        Assert.Equal(6, completed.Value);
        Assert.Equal(0, ctx.Sequence.Calls);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(WorkRecordStatus.Completed, reloaded.Status);
        Assert.Equal(2, reloaded.Attempts.Count);
        Assert.Equal(6, reloaded.History.Count);
    }
}
