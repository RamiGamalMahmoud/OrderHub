using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;
using OrderHub.Infrastructure;

namespace OrderHub.Tests;

public sealed class StandaloneTripPersistenceTests
{
    [Fact]
    public async Task Trip_graph_round_trips_with_attempts_and_history()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        trip.Start(db.Operation());
        trip.Fail("فشل أول", db.Operation(TripTestDb.AtUtc.AddMinutes(5)));
        trip.Retry(db.Operation(TripTestDb.AtUtc.AddMinutes(10)));
        await db.Context.SaveChangesAsync();

        db.Context.ChangeTracker.Clear();
        var reloaded = await db.Context.StandaloneTrips
            .Include(item => item.Attempts)
            .Include(item => item.History)
            .SingleAsync(item => item.Id == trip.Id);

        Assert.Equal(4, reloaded.Version);
        Assert.Equal(WorkRecordStatus.Pending, reloaded.Status);
        Assert.Equal(TripDisplayState.AwaitingRetryStart, reloaded.GetDisplayState());
        Assert.Equal(2, reloaded.Attempts.Count);
        Assert.All(reloaded.Attempts, attempt => Assert.Equal(reloaded.Id, attempt.TripId));
        Assert.Equal(
            new[]
            {
                StandaloneTripEventType.Created,
                StandaloneTripEventType.AttemptStarted,
                StandaloneTripEventType.Failed,
                StandaloneTripEventType.Retried
            },
            reloaded.History.OrderBy(entry => entry.Id).Select(entry => entry.EventType).ToArray());
        Assert.All(reloaded.History, entry => Assert.Equal(reloaded.Id, entry.TripId));
        Assert.All(reloaded.History, entry => Assert.Equal(db.User.Id, entry.ChangedByUserId));
    }

    [Fact]
    public async Task Duplicate_trip_number_for_same_deliveryman_and_month_is_rejected()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1);

        var duplicate = StandaloneTrip.Create(
            db.Deliveryman.Id,
            "مشوار مكرر",
            null,
            TripTestDb.ExternalParty("من"),
            TripTestDb.ExternalParty("إلى"),
            null,
            db.Operation());
        duplicate.AssignNumber(1, TripTestDb.SequenceYear, TripTestDb.SequenceMonth);
        db.Context.StandaloneTrips.Add(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Same_number_is_allowed_for_another_deliveryman()
    {
        using var db = new TripTestDb();
        db.PersistTrip(1);

        var other = db.PersistTrip(1, deliveryman: db.OtherDeliveryman);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(2, await db.Context.StandaloneTrips.CountAsync(item => item.TripNumber == 1));
        Assert.Equal(db.OtherDeliveryman.Id, other.DeliverymanId);
    }

    [Fact]
    public async Task Partial_unique_index_blocks_a_second_open_attempt()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);

        var first = StandaloneTripAttempt.Pending(2, TripTestDb.AtUtc);
        var second = StandaloneTripAttempt.Pending(3, TripTestDb.AtUtc);
        SetTripId(first, trip.Id);
        SetTripId(second, trip.Id);
        db.Context.StandaloneTripAttempts.AddRange(first, second);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task History_entries_cannot_be_modified_or_deleted()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        db.Context.ChangeTracker.Clear();

        var loaded = await db.Context.StandaloneTrips
            .Include(item => item.History)
            .SingleAsync(item => item.Id == trip.Id);
        var entry = loaded.History.Single();
        db.Context.Entry(entry).Property(item => item.Reason).CurrentValue = "tampered";

        var update = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Context.SaveChangesAsync());
        Assert.Contains("سجل التشغيل", update.Message);
    }

    [Fact]
    public async Task History_delete_is_blocked()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        db.Context.ChangeTracker.Clear();

        var loaded = await db.Context.StandaloneTrips
            .Include(item => item.History)
            .SingleAsync(item => item.Id == trip.Id);
        db.Context.Remove(loaded.History.Single());

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Version_is_a_concurrency_token()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);

        await db.Context.Database.ExecuteSqlRawAsync(
            "UPDATE standalone_trips SET version = version + 1 WHERE id = {0}",
            trip.Id);

        db.Context.Entry(trip).Property(item => item.Title).CurrentValue = "عنوان متعارض";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Twenty_two_character_phone_snapshot_persists_intact()
    {
        using var db = new TripTestDb();
        const string phone = "0501234567890123456789";
        var trip = db.PersistTrip(
            1,
            from: TripTestDb.ExternalParty("مرسل بهاتف طويل", phone));

        db.Context.ChangeTracker.Clear();
        var reloaded = await db.Context.StandaloneTrips.SingleAsync(item => item.Id == trip.Id);

        Assert.Equal(22, reloaded.From.Phone!.Length);
        Assert.Equal(phone, reloaded.From.Phone);
    }

    [Fact]
    public async Task External_party_check_constraint_rejects_missing_phone()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);

        var owned = db.Context.Entry(trip).Reference(item => item.From).TargetEntry!;
        owned.Property(item => item.Phone).CurrentValue = null;

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Trip_number_check_constraint_rejects_unassigned_numbers()
    {
        using var db = new TripTestDb();
        var trip = StandaloneTrip.Create(
            db.Deliveryman.Id,
            "بدون رقم",
            null,
            TripTestDb.ExternalParty("من"),
            TripTestDb.ExternalParty("إلى"),
            null,
            db.Operation());
        db.Context.StandaloneTrips.Add(trip);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    private static void SetTripId(StandaloneTripAttempt attempt, int tripId) =>
        typeof(StandaloneTripAttempt)
            .GetProperty(nameof(StandaloneTripAttempt.TripId))!
            .SetValue(attempt, tripId);
}
