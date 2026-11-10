using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Features.Trips.UpdateTrip;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Tests;

public sealed class UpdateTripHandlerTests
{
    [Fact]
    public async Task Pure_edit_updates_fields_and_writes_one_edited_history_row()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.UpdateHandler().Handle(
            Command(db, trip.Id, trip.Version, title: "عنوان محدث", editNote: "تعديل سريع"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(0, ctx.Sequence.Calls);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal("عنوان محدث", reloaded.Title);
        Assert.Equal(2, reloaded.Version);
        Assert.Equal(db.Deliveryman.Id, reloaded.DeliverymanId);
        Assert.Equal(1L, reloaded.TripNumber);
        Assert.Equal(TripTestDb.SequenceYear, reloaded.SequenceYear);
        Assert.Equal(TripTestDb.SequenceMonth, reloaded.SequenceMonth);

        Assert.Equal(2, reloaded.History.Count);
        var edited = reloaded.History.Single(entry => entry.EventType == StandaloneTripEventType.Edited);
        Assert.Equal("تعديل سريع", edited.Reason);
        Assert.DoesNotContain(
            reloaded.History,
            entry => entry.EventType == StandaloneTripEventType.Reassigned);
    }

    [Fact]
    public async Task Reassignment_allocates_from_creation_month_and_keeps_sequence_period()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var createdAt = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var trip = db.PersistTrip(1, createdAtUtc: createdAt);
        ctx.Sequence.NextNumber = 44;

        var result = await ctx.UpdateHandler().Handle(
            Command(
                db,
                trip.Id,
                trip.Version,
                deliverymanId: db.OtherDeliveryman.Id,
                reassignReason: "تغيير المندوب"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stamp = Assert.Single(ctx.Sequence.Timestamps);
        Assert.Equal(db.OtherDeliveryman.Id, Assert.Single(ctx.Sequence.PartyIds));
        Assert.Equal(2025, stamp.UtcDateTime.Year);
        Assert.Equal(3, stamp.UtcDateTime.Month);
        Assert.Equal(createdAt, stamp.UtcDateTime);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(2, reloaded.Version);
        Assert.Equal(db.OtherDeliveryman.Id, reloaded.DeliverymanId);
        Assert.Equal(44L, reloaded.TripNumber);
        Assert.Equal(TripTestDb.SequenceYear, reloaded.SequenceYear);
        Assert.Equal(TripTestDb.SequenceMonth, reloaded.SequenceMonth);

        Assert.Equal(2, reloaded.History.Count);
        var reassigned = reloaded.History.Single(entry =>
            entry.EventType == StandaloneTripEventType.Reassigned);
        Assert.Equal(db.Deliveryman.Id, reassigned.PreviousDeliverymanId);
        Assert.Equal(db.OtherDeliveryman.Id, reassigned.NewDeliverymanId);
        Assert.Equal("سالم", reassigned.PreviousDeliverymanName);
        Assert.Equal("ناصر", reassigned.NewDeliverymanName);
        Assert.Equal(1L, reassigned.PreviousTripNumber);
        Assert.Equal(44L, reassigned.NewTripNumber);
        Assert.Equal("تغيير المندوب", reassigned.Reason);
    }

    [Fact]
    public async Task Stale_version_returns_conflict_and_writes_nothing()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.UpdateHandler().Handle(
            Command(db, trip.Id, trip.Version + 1, title: "لن يمر"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Contains("أعد تحميل البيانات", result.ErrorMessage);
        Assert.Equal(0, ctx.Sequence.Calls);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal("مشوار تجريبي", reloaded.Title);
        Assert.Equal(1, reloaded.Version);
    }

    [Fact]
    public async Task Edit_is_rejected_after_the_first_attempt_started()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        trip.Start(db.Operation());
        db.Context.SaveChanges();

        var result = await ctx.UpdateHandler().Handle(
            Command(db, trip.Id, trip.Version, title: "بعد البدء"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Contains("بعد بدء المحاولة الأولى", result.ErrorMessage);
        Assert.Equal(0, ctx.Sequence.Calls);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal("مشوار تجريبي", reloaded.Title);
        Assert.Equal(2, reloaded.Version);
        Assert.Single(reloaded.Attempts);
    }

    [Fact]
    public async Task Reassignment_without_reason_is_rejected_before_numbering()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.UpdateHandler().Handle(
            Command(db, trip.Id, trip.Version, deliverymanId: db.OtherDeliveryman.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Contains("سبب إعادة الإسناد", result.ErrorMessage);
        Assert.Equal(0, ctx.Sequence.Calls);

        db.Context.ChangeTracker.Clear();
        Assert.Equal(db.Deliveryman.Id, db.Reload(trip.Id).DeliverymanId);
    }

    [Fact]
    public async Task Reassignment_to_a_missing_deliveryman_returns_not_found()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.UpdateHandler().Handle(
            Command(db, trip.Id, trip.Version, deliverymanId: 4242, reassignReason: "اختبار"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
        Assert.True(result.ErrorMessage.Contains("المندوب غير موجود."), result.ErrorMessage);
        Assert.Equal(0, ctx.Sequence.Calls);
    }

    [Fact]
    public async Task Edit_note_longer_than_two_hundred_characters_is_rejected()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.UpdateHandler().Handle(
            Command(db, trip.Id, trip.Version, editNote: new string('ن', 201)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Contains("ملاحظة التعديل", result.ErrorMessage);
        Assert.Equal(0, ctx.Sequence.Calls);
    }

    [Fact]
    public async Task Untouched_party_snapshots_are_preserved()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(
            1,
            from: TripParty.FromClient(db.Client),
            to: TripTestDb.ExternalParty("المستلم", "0507777777", "حي الورود"));
        var originalFromPhone = trip.From.Phone;
        var originalFromAddress = trip.From.Address;
        var originalToPhone = trip.To.Phone;
        var originalToAddress = trip.To.Address;

        var result = await ctx.UpdateHandler().Handle(
            Command(
                db,
                trip.Id,
                trip.Version,
                title: "تعديل العنوان فقط",
                to: ExternalInput("المستلم", "0507777777", "حي الورود")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(originalFromPhone, reloaded.From.Phone);
        Assert.Equal(originalFromAddress, reloaded.From.Address);
        Assert.Equal(originalToPhone, reloaded.To.Phone);
        Assert.Equal(originalToAddress, reloaded.To.Address);
    }

    [Fact]
    public async Task Rejected_reassignment_save_leaves_the_trip_and_number_untouched()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);
        db.PersistTrip(44, deliveryman: db.OtherDeliveryman, title: "مشوار ناصر");
        ctx.Sequence.NextNumber = 44;

        var result = await ctx.UpdateHandler().Handle(
            Command(db, trip.Id, trip.Version, deliverymanId: db.OtherDeliveryman.Id, reassignReason: "نقل"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Persistence", result.ErrorCode);

        db.Context.ChangeTracker.Clear();
        var reloaded = db.Reload(trip.Id);
        Assert.Equal(db.Deliveryman.Id, reloaded.DeliverymanId);
        Assert.Equal(1L, reloaded.TripNumber);
        Assert.Equal(1, reloaded.Version);
        Assert.Single(reloaded.History);
    }

    [Fact]
    public async Task Unknown_trip_returns_not_found()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);

        var result = await ctx.UpdateHandler().Handle(
            Command(db, 4242, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
    }

    [Fact]
    public async Task Unauthenticated_user_is_rejected()
    {
        using var db = new TripTestDb();
        var ctx = new TripHandlerTestContext(db);
        var trip = db.PersistTrip(1);

        var result = await ctx.UpdateHandler(TestCurrentUser.Anonymous).Handle(
            Command(db, trip.Id, trip.Version),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.ErrorCode);
        Assert.Equal(0, ctx.Sequence.Calls);
    }

    private static UpdateTripCommand Command(
        TripTestDb db,
        int id,
        int expectedVersion,
        int? deliverymanId = null,
        string title = "زيارة عميل",
        TripPartyInput? from = null,
        TripPartyInput? to = null,
        DateTimeOffset? scheduledAtUtc = null,
        string? editNote = null,
        string? reassignReason = null) =>
        new(
            id,
            expectedVersion,
            deliverymanId ?? db.Deliveryman.Id,
            title,
            null,
            from ?? new TripPartyInput(TripPartyType.Client, db.Client.Id, null, null, null, null),
            to ?? ExternalInput("المستلم"),
            scheduledAtUtc,
            editNote,
            reassignReason);

    private static TripPartyInput ExternalInput(
        string name,
        string phone = "0505555555",
        string address = "حي النخيل") =>
        new(TripPartyType.External, null, name, phone, address, null);
}
