using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Trips.CreateTrip;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Tests;

public sealed class CreateTripHandlerTests
{
    [Fact]
    public async Task Create_persists_snapshots_history_and_numbering_period()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var result = await ctx.CreateHandler().Handle(Command(db), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var receipt = result.Value;
        Assert.True(receipt.Id > 0);
        Assert.Equal(1, receipt.Version);
        Assert.Equal(WorkRecordStatus.Pending, receipt.Status);
        Assert.Equal(1, receipt.TripNumber);
        Assert.Equal(TripTestDb.SequenceYear, receipt.SequenceYear);
        Assert.Equal(TripTestDb.SequenceMonth, receipt.SequenceMonth);

        var partyId = Assert.Single(ctx.Sequence.PartyIds);
        Assert.Equal(db.Deliveryman.Id, partyId);
        var stamp = Assert.Single(ctx.Sequence.Timestamps);
        Assert.Equal(TimeSpan.Zero, stamp.Offset);
        Assert.Equal(TripTestDb.AtUtc, stamp.UtcDateTime);

        db.Context.ChangeTracker.Clear();
        var trip = db.Context.StandaloneTrips
            .Include(item => item.History)
            .Single(item => item.Id == receipt.Id);

        Assert.Equal(TripTestDb.AtUtc, trip.CreatedAt);
        Assert.Equal(TripTestDb.AtUtc, trip.StatusChangedAtUtc);
        Assert.Equal(db.Client.Id, trip.From.PartyId);
        Assert.Equal(TripPartyType.Client, trip.From.PartyType);
        Assert.Equal("أحمد", trip.From.Name);
        Assert.Equal("+966 - 0501111111", trip.From.Phone);
        Assert.Equal("الرياض - شارع الملك", trip.From.Address);
        Assert.Equal(1, CountOccurrences(trip.From.Address!, "الرياض"));
        Assert.Equal("المستلم", trip.To.Name);
        Assert.Equal("0505555555", trip.To.Phone);
        Assert.Equal("حي النخيل", trip.To.Address);

        var created = Assert.Single(trip.History);
        Assert.Equal(StandaloneTripEventType.Created, created.EventType);
        Assert.Equal(WorkRecordStatus.Pending, created.NewStatus);
        Assert.Equal(db.User.Id, created.ChangedByUserId);
        Assert.Equal("مستخدم الاختبار", created.ChangedByUserName);
        Assert.Equal(TripTestDb.AtUtc, created.ChangedAtUtc);
    }

    [Fact]
    public async Task Unauthenticated_user_is_rejected_before_any_work()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var result = await ctx.CreateHandler(TestCurrentUser.Anonymous)
            .Handle(Command(db), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.ErrorCode);
        Assert.Equal(0, ctx.Sequence.Calls);
        Assert.Empty(db.Context.StandaloneTrips);
    }

    [Fact]
    public async Task Validation_failure_consumes_no_sequence_number()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var result = await ctx.CreateHandler()
            .Handle(Command(db, title: "   "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Contains("العنوان مطلوب", result.ErrorMessage);
        Assert.Equal(0, ctx.Sequence.Calls);
        Assert.Empty(db.Context.StandaloneTrips);
    }

    [Fact]
    public async Task Missing_deliveryman_returns_not_found_without_numbering()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var result = await ctx.CreateHandler()
            .Handle(Command(db, deliverymanId: 4242), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
        Assert.Equal("المندوب غير موجود.", result.ErrorMessage);
        Assert.Equal(0, ctx.Sequence.Calls);
    }

    [Fact]
    public async Task Missing_party_record_returns_invalid_operation_without_numbering()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var result = await ctx.CreateHandler()
            .Handle(Command(db, to: ClientInput(db, 4242)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Equal("العميل غير موجود.", result.ErrorMessage);
        Assert.Equal(0, ctx.Sequence.Calls);
    }

    [Fact]
    public async Task Twenty_two_character_phone_snapshot_persists_intact()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        const string phone = "0501234567890123456789";

        var result = await ctx.CreateHandler().Handle(
            Command(db, to: ExternalInput("المستلم", phone: phone)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        db.Context.ChangeTracker.Clear();
        var trip = await db.Context.StandaloneTrips.SingleAsync(item => item.Id == result.Value.Id);
        Assert.Equal(22, phone.Length);
        Assert.Equal(phone, trip.To.Phone);
    }

    [Fact]
    public async Task Failed_save_rolls_back_the_trip_and_leaves_the_sequence_reusable()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var first = await ctx.CreateHandler().Handle(Command(db, title: "الأول"), CancellationToken.None);
        Assert.True(first.IsSuccess);

        ctx.Sequence.NextNumber = 1;
        var duplicate = await ctx.CreateHandler().Handle(Command(db, title: "مكرر"), CancellationToken.None);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal("Persistence", duplicate.ErrorCode);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(1, db.Context.StandaloneTrips.Count());
        Assert.Equal(1, db.Context.StandaloneTripHistoryEntries.Count());

        ctx.Sequence.NextNumber = 2;
        var third = await ctx.CreateHandler().Handle(Command(db, title: "الثالث"), CancellationToken.None);
        Assert.True(third.IsSuccess);
        Assert.Equal(2, third.Value.TripNumber);
    }

    [Fact]
    public async Task Numbering_failure_rolls_back_and_rethrows()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        ctx.Sequence.Failure = new InvalidOperationException("sequence offline");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ctx.CreateHandler().Handle(Command(db), CancellationToken.None));

        db.Context.ChangeTracker.Clear();
        Assert.Empty(db.Context.StandaloneTrips);
    }

    [Fact]
    public async Task Scheduled_time_is_stored_in_utc()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var riyadh = new DateTimeOffset(2026, 11, 12, 9, 30, 0, TimeSpan.FromHours(3));

        var result = await ctx.CreateHandler()
            .Handle(Command(db, scheduledAtUtc: riyadh), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
        var trip = await db.Context.StandaloneTrips.SingleAsync(item => item.Id == result.Value.Id);
        Assert.Equal(new DateTime(2026, 11, 12, 6, 30, 0, DateTimeKind.Utc), trip.ScheduledAtUtc);
    }

    [Fact]
    public async Task Title_is_trimmed_and_blank_description_is_stored_as_null()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var command = Command(db, title: "  زيارة صباحية  ") with { Description = "   " };
        var result = await ctx.CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
        var trip = await db.Context.StandaloneTrips.SingleAsync(item => item.Id == result.Value.Id);
        Assert.Equal("زيارة صباحية", trip.Title);
        Assert.Null(trip.Description);
    }

    private static CreateTripCommand Command(
        TripTestDb db,
        TripPartyInput? from = null,
        TripPartyInput? to = null,
        string title = "زيارة عميل",
        DateTimeOffset? scheduledAtUtc = null,
        int? deliverymanId = null) =>
        new(
            deliverymanId ?? db.Deliveryman.Id,
            title,
            null,
            from ?? ClientInput(db),
            to ?? ExternalInput("المستلم"),
            scheduledAtUtc);

    private static TripPartyInput ClientInput(TripTestDb db, int? id = null) =>
        new(TripPartyType.Client, id ?? db.Client.Id, null, null, null, null);

    private static TripPartyInput ExternalInput(
        string name,
        string phone = "0505555555",
        string address = "حي النخيل") =>
        new(TripPartyType.External, null, name, phone, address, null);

    private static int CountOccurrences(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;
}
