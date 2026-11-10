using System;
using System.Linq;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Tests;

public sealed class StandaloneTripDomainTests
{
    private static readonly TripOperation Operation = new(1, "مستخدم", TripTestDb.AtUtc);

    [Fact]
    public void Create_initializes_pending_new_trip_with_creation_history()
    {
        var trip = NewTrip();

        Assert.Equal(WorkRecordStatus.Pending, trip.Status);
        Assert.Equal(1, trip.Version);
        Assert.Null(trip.StartedAtUtc);
        Assert.Empty(trip.Attempts);
        Assert.True(trip.IsNew);
        Assert.Equal(TripDisplayState.New, trip.GetDisplayState());
        var history = Assert.Single(trip.History);
        Assert.Equal(StandaloneTripEventType.Created, history.EventType);
        Assert.Equal(WorkRecordStatus.Pending, history.NewStatus);
        Assert.Null(history.PreviousStatus);
    }

    [Theory]
    [InlineData(0, "عنوان")]
    [InlineData(5, "  ")]
    [InlineData(5, null)]
    public void Create_rejects_invalid_identity_and_title(int deliverymanId, string? title) =>
        Assert.Throws<InvalidOperationException>(() =>
            StandaloneTrip.Create(
                deliverymanId,
                title,
                null,
                TripTestDb.ExternalParty("من"),
                TripTestDb.ExternalParty("إلى"),
                null,
                Operation));

    [Fact]
    public void Create_rejects_overlong_title_and_description()
    {
        Assert.Throws<InvalidOperationException>(() => StandaloneTrip.Create(
            1, new string('ا', 151), null, TripTestDb.ExternalParty("من"), TripTestDb.ExternalParty("إلى"), null, Operation));

        Assert.Throws<InvalidOperationException>(() => StandaloneTrip.Create(
            1, "عنوان", new string('ا', 1001), TripTestDb.ExternalParty("من"), TripTestDb.ExternalParty("إلى"), null, Operation));
    }

    [Fact]
    public void AssignNumber_is_one_time_and_validates_inputs()
    {
        var trip = NewTrip();

        Assert.Throws<ArgumentException>(() => trip.AssignNumber(0, 2026, 11));
        Assert.Throws<ArgumentException>(() => trip.AssignNumber(1, 2026, 13));

        trip.AssignNumber(12, 2026, 11);
        Assert.Equal(12, trip.TripNumber);
        Assert.Equal(2026, trip.SequenceYear);
        Assert.Equal(11, trip.SequenceMonth);
        Assert.Throws<InvalidOperationException>(() => trip.AssignNumber(13, 2026, 11));
        Assert.Equal(1, trip.Version);
    }

    [Fact]
    public void Start_creates_first_attempt_and_sets_started_at_once()
    {
        var trip = NewTrip();

        trip.Start(Operation);

        Assert.Equal(2, trip.Version);
        Assert.Equal(TripTestDb.AtUtc, trip.StartedAtUtc);
        var attempt = Assert.Single(trip.Attempts);
        Assert.Equal(1, attempt.AttemptNumber);
        Assert.Equal(WorkRecordStatus.Pending, attempt.Status);
        Assert.Equal(TripTestDb.AtUtc, attempt.StartedAtUtc);
        Assert.Equal(TripDisplayState.InProgress, trip.GetDisplayState());
        Assert.False(trip.IsNew);
        Assert.Equal(1, trip.LatestAttemptNumber);
        Assert.Equal(StandaloneTripEventType.AttemptStarted, trip.History.Last().EventType);
    }

    [Fact]
    public void Complete_requires_started_open_attempt_and_closes_it()
    {
        var trip = NewTrip();

        Assert.Throws<InvalidOperationException>(() => trip.Complete(Operation));

        trip.Start(Operation);
        trip.Complete(Operation);

        Assert.Equal(3, trip.Version);
        Assert.Equal(WorkRecordStatus.Completed, trip.Status);
        Assert.Equal(TripDisplayState.Completed, trip.GetDisplayState());
        var attempt = Assert.Single(trip.Attempts);
        Assert.Equal(WorkRecordStatus.Completed, attempt.Status);
        Assert.Equal(TripTestDb.AtUtc, attempt.EndedAtUtc);
        Assert.Null(attempt.Reason);
    }

    [Fact]
    public void Fail_requires_reason_and_started_attempt()
    {
        var trip = NewTrip();
        Assert.Throws<InvalidOperationException>(() => trip.Fail("سبب", Operation));

        trip.Start(Operation);
        Assert.Throws<InvalidOperationException>(() => trip.Fail(null, Operation));
        Assert.Throws<InvalidOperationException>(() => trip.Fail("   ", Operation));

        trip.Fail("العميل رفض الاستلام", Operation);

        Assert.Equal(3, trip.Version);
        Assert.Equal(WorkRecordStatus.Failed, trip.Status);
        Assert.Equal(TripDisplayState.Failed, trip.GetDisplayState());
        var attempt = Assert.Single(trip.Attempts);
        Assert.Equal(WorkRecordStatus.Failed, attempt.Status);
        Assert.Equal("العميل رفض الاستلام", attempt.Reason);
        Assert.Equal(StandaloneTripEventType.Failed, trip.History.Last().EventType);
    }

    [Fact]
    public void Retry_creates_unstarted_pending_attempt_and_returns_header_to_pending()
    {
        var trip = NewTrip();
        trip.Start(Operation);
        trip.Fail("محاولة أولى فاشلة", Operation);

        trip.Retry(Operation);

        Assert.Equal(4, trip.Version);
        Assert.Equal(WorkRecordStatus.Pending, trip.Status);
        Assert.Equal(TripDisplayState.AwaitingRetryStart, trip.GetDisplayState());
        Assert.Equal(2, trip.Attempts.Count);
        var retryAttempt = trip.Attempts.Single(attempt => attempt.AttemptNumber == 2);
        Assert.Equal(WorkRecordStatus.Pending, retryAttempt.Status);
        Assert.Null(retryAttempt.StartedAtUtc);
        Assert.Equal(2, trip.LatestAttemptNumber);
        Assert.Equal(StandaloneTripEventType.Retried, trip.History.Last().EventType);
        Assert.Single(trip.Attempts, attempt => attempt.Status == WorkRecordStatus.Pending);

        trip.Start(Operation);
        Assert.Equal(TripDisplayState.InProgress, trip.GetDisplayState());
        Assert.NotNull(retryAttempt.StartedAtUtc);

        trip.Complete(Operation);
        Assert.Equal(WorkRecordStatus.Completed, trip.Status);
        Assert.Equal(1, trip.Attempts.Count(attempt => attempt.Status == WorkRecordStatus.Completed));
        Assert.Equal(1, trip.Attempts.Count(attempt => attempt.Status == WorkRecordStatus.Failed));
    }

    [Fact]
    public void Retry_requires_failed_header_and_no_open_attempt()
    {
        var trip = NewTrip();

        Assert.Throws<InvalidOperationException>(() => trip.Retry(Operation));

        trip.Start(Operation);
        trip.Fail("فشل", Operation);
        trip.Retry(Operation);
        Assert.Throws<InvalidOperationException>(() => trip.Retry(Operation));
    }

    [Fact]
    public void Cancel_without_attempt_records_history_with_null_attempt()
    {
        var trip = NewTrip();

        Assert.Throws<InvalidOperationException>(() => trip.Cancel(" ", Operation));

        trip.Cancel("ألغي قبل البدء", Operation);

        Assert.Equal(2, trip.Version);
        Assert.Equal(WorkRecordStatus.Cancelled, trip.Status);
        Assert.Equal(TripDisplayState.Cancelled, trip.GetDisplayState());
        Assert.Empty(trip.Attempts);
        var history = trip.History.Last();
        Assert.Equal(StandaloneTripEventType.Cancelled, history.EventType);
        Assert.Null(history.AttemptNumber);
        Assert.Equal("ألغي قبل البدء", history.Reason);
    }

    [Fact]
    public void Cancel_from_in_progress_closes_the_open_attempt()
    {
        var trip = NewTrip();
        trip.Start(Operation);

        trip.Cancel("طلب العميل الإلغاء", Operation);

        Assert.Equal(3, trip.Version);
        Assert.Equal(WorkRecordStatus.Cancelled, trip.Status);
        var attempt = Assert.Single(trip.Attempts);
        Assert.Equal(WorkRecordStatus.Cancelled, attempt.Status);
        Assert.Equal("طلب العميل الإلغاء", attempt.Reason);
        Assert.Equal(1, trip.History.Last().AttemptNumber);
    }

    [Fact]
    public void Cancel_from_failed_is_allowed()
    {
        var trip = NewTrip();
        trip.Start(Operation);
        trip.Fail("فشل", Operation);

        trip.Cancel("إغلاق بعد الفشل", Operation);

        Assert.Equal(WorkRecordStatus.Cancelled, trip.Status);
        Assert.Equal(4, trip.Version);
        Assert.DoesNotContain(trip.Attempts, attempt => attempt.Status == WorkRecordStatus.Pending);
    }

    [Fact]
    public void Terminal_states_reject_every_mutation()
    {
        var completed = NewTrip();
        completed.Start(Operation);
        completed.Complete(Operation);
        AssertTerminal(completed);

        var cancelled = NewTrip();
        cancelled.Cancel("إلغاء", Operation);
        AssertTerminal(cancelled);
    }

    [Fact]
    public void Update_rejects_any_change_after_the_first_start()
    {
        var trip = NewTrip();
        trip.Start(Operation);

        Assert.Throws<InvalidOperationException>(() =>
            trip.Update("عنوان", null, trip.From, trip.To, null, trip.DeliverymanId, null, null, null, Operation));
    }

    [Fact]
    public void Update_pure_edit_changes_fields_and_writes_single_edited_history()
    {
        var trip = NewTrip();
        var from = TripTestDb.ExternalParty("المرسل الجديد");
        var to = TripTestDb.ExternalParty("المستلم الجديد");
        var scheduled = new DateTimeOffset(2026, 11, 12, 6, 0, 0, TimeSpan.Zero);

        trip.Update("عنوان محدث", "وصف محدث", from, to, scheduled, trip.DeliverymanId, null, null, "ملاحظة تعديل", Operation);

        Assert.Equal(2, trip.Version);
        Assert.Equal("عنوان محدث", trip.Title);
        Assert.Equal("وصف محدث", trip.Description);
        Assert.Equal("المرسل الجديد", trip.From.Name);
        Assert.Equal("المستلم الجديد", trip.To.Name);
        Assert.Equal(scheduled.UtcDateTime, trip.ScheduledAtUtc);
        Assert.Equal(2, trip.History.Count);
        var edited = trip.History.Last();
        Assert.Equal(StandaloneTripEventType.Edited, edited.EventType);
        Assert.Equal("ملاحظة تعديل", edited.Reason);
    }

    [Fact]
    public void Update_pure_edit_rejects_reassignment_payload_without_deliveryman_change()
    {
        var trip = NewTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.Update("عنوان", null, trip.From, trip.To, null, trip.DeliverymanId, "اسم", null, "سبب", Operation));
        Assert.Throws<InvalidOperationException>(() =>
            trip.Update("عنوان", null, trip.From, trip.To, null, trip.DeliverymanId, null, 5, "سبب", Operation));
    }

    [Fact]
    public void Update_reassignment_renumbers_and_writes_single_reassigned_history()
    {
        using var db = new TripTestDb();
        var persisted = db.PersistTrip(7);
        var trip = db.ReloadWithDeliveryman(persisted.Id);
        long? sequenceYear = trip.SequenceYear;
        long? sequenceMonth = trip.SequenceMonth;

        Assert.Throws<InvalidOperationException>(() =>
            trip.Update("عنوان", null, trip.From, trip.To, null, db.OtherDeliveryman.Id, "ناصر", 9, "  ", db.Operation()));

        trip.Update("عنوان", null, trip.From, trip.To, null, db.OtherDeliveryman.Id, "ناصر", 9, "نقل للمندوب الآخر", db.Operation());

        Assert.Equal(2, trip.Version);
        Assert.Equal(db.OtherDeliveryman.Id, trip.DeliverymanId);
        Assert.Equal(9, trip.TripNumber);
        Assert.Equal(sequenceYear, trip.SequenceYear);
        Assert.Equal(sequenceMonth, trip.SequenceMonth);
        Assert.Equal(TripDisplayState.New, trip.GetDisplayState());
        Assert.Equal(2, trip.History.Count);
        var reassigned = trip.History.Last();
        Assert.Equal(StandaloneTripEventType.Reassigned, reassigned.EventType);
        Assert.Equal(7, reassigned.PreviousTripNumber);
        Assert.Equal(9, reassigned.NewTripNumber);
        Assert.Equal("سالم", reassigned.PreviousDeliverymanName);
        Assert.Equal("ناصر", reassigned.NewDeliverymanName);
        Assert.Equal("نقل للمندوب الآخر", reassigned.Reason);
    }

    [Fact]
    public void Update_reassignment_rejects_missing_new_deliveryman_name_or_number()
    {
        using var db = new TripTestDb();
        var trip = db.ReloadWithDeliveryman(db.PersistTrip(3).Id);

        Assert.Throws<InvalidOperationException>(() =>
            trip.Update("عنوان", null, trip.From, trip.To, null, db.OtherDeliveryman.Id, " ", 4, "سبب", db.Operation()));
        Assert.Throws<InvalidOperationException>(() =>
            trip.Update("عنوان", null, trip.From, trip.To, null, db.OtherDeliveryman.Id, "ناصر", null, "سبب", db.Operation()));
    }

    [Fact]
    public void Version_increments_exactly_once_per_mutation()
    {
        var trip = NewTrip();
        var historyCount = trip.History.Count;

        trip.Update("عنوان", null, trip.From, trip.To, null, trip.DeliverymanId, null, null, null, Operation);
        Assert.Equal(2, trip.Version);
        trip.Start(Operation);
        Assert.Equal(3, trip.Version);
        trip.Fail("فشل", Operation);
        Assert.Equal(4, trip.Version);
        trip.Retry(Operation);
        Assert.Equal(5, trip.Version);
        trip.Start(Operation);
        Assert.Equal(6, trip.Version);
        trip.Complete(Operation);
        Assert.Equal(7, trip.Version);

        Assert.Equal(historyCount + 6, trip.History.Count);
        Assert.Throws<InvalidOperationException>(() => trip.Complete(Operation));
        Assert.Equal(7, trip.Version);
        Assert.Equal(historyCount + 6, trip.History.Count);
    }

    private static StandaloneTrip NewTrip() =>
        StandaloneTrip.Create(
            5,
            "مشوار اختبار",
            "وصف",
            TripTestDb.ExternalParty("المرسل"),
            TripTestDb.ExternalParty("المستلم"),
            null,
            Operation);

    private static void AssertTerminal(StandaloneTrip trip)
    {
        Assert.Throws<InvalidOperationException>(() => trip.Start(Operation));
        Assert.Throws<InvalidOperationException>(() => trip.Complete(Operation));
        Assert.Throws<InvalidOperationException>(() => trip.Fail("سبب", Operation));
        Assert.Throws<InvalidOperationException>(() => trip.Cancel("سبب", Operation));
        Assert.Throws<InvalidOperationException>(() => trip.Retry(Operation));
        Assert.Throws<InvalidOperationException>(() =>
            trip.Update("عنوان", null, trip.From, trip.To, null, trip.DeliverymanId, null, null, null, Operation));
    }
}
