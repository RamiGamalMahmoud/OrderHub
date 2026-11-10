using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Domain.Models.Trips;

public class StandaloneTripHistoryEntry
{
    public int Id { get; private set; }
    public int TripId { get; private set; }
    public StandaloneTripEventType EventType { get; private set; }
    public int? AttemptNumber { get; private set; }
    public WorkRecordStatus? PreviousStatus { get; private set; }
    public WorkRecordStatus? NewStatus { get; private set; }
    public int? PreviousDeliverymanId { get; private set; }
    public int? NewDeliverymanId { get; private set; }
    public string? PreviousDeliverymanName { get; private set; }
    public string? NewDeliverymanName { get; private set; }
    public long? PreviousTripNumber { get; private set; }
    public long? NewTripNumber { get; private set; }
    public string? Reason { get; private set; }
    public int ChangedByUserId { get; private set; }
    public string ChangedByUserName { get; private set; } = null!;
    public DateTime ChangedAtUtc { get; private set; }
    public string Source { get; private set; } = null!;

    public static StandaloneTripHistoryEntry Created(
        int tripId,
        TripOperation operation,
        WorkRecordStatus? previousStatus,
        WorkRecordStatus newStatus)
    {
        ValidateOperation(operation);

        return new StandaloneTripHistoryEntry
        {
            TripId = tripId,
            EventType = StandaloneTripEventType.Created,
            AttemptNumber = null,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = operation.UserId,
            ChangedByUserName = operation.UserName,
            ChangedAtUtc = operation.AtUtc,
            Source = operation.Source
        };
    }

    public static StandaloneTripHistoryEntry AttemptStarted(
        int tripId,
        TripOperation operation,
        int attemptNumber)
    {
        ValidateOperation(operation);

        return new StandaloneTripHistoryEntry
        {
            TripId = tripId,
            EventType = StandaloneTripEventType.AttemptStarted,
            AttemptNumber = attemptNumber,
            ChangedByUserId = operation.UserId,
            ChangedByUserName = operation.UserName,
            ChangedAtUtc = operation.AtUtc,
            Source = operation.Source
        };
    }

    public static StandaloneTripHistoryEntry Completed(
        int tripId,
        TripOperation operation,
        int attemptNumber,
        WorkRecordStatus previousStatus,
        WorkRecordStatus newStatus)
    {
        ValidateOperation(operation);

        return new StandaloneTripHistoryEntry
        {
            TripId = tripId,
            EventType = StandaloneTripEventType.Completed,
            AttemptNumber = attemptNumber,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = operation.UserId,
            ChangedByUserName = operation.UserName,
            ChangedAtUtc = operation.AtUtc,
            Source = operation.Source
        };
    }

    public static StandaloneTripHistoryEntry Failed(
        int tripId,
        TripOperation operation,
        int attemptNumber,
        string reason,
        WorkRecordStatus previousStatus,
        WorkRecordStatus newStatus)
    {
        ValidateOperation(operation);
        ValidateReason(reason);

        return new StandaloneTripHistoryEntry
        {
            TripId = tripId,
            EventType = StandaloneTripEventType.Failed,
            AttemptNumber = attemptNumber,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = operation.UserId,
            ChangedByUserName = operation.UserName,
            ChangedAtUtc = operation.AtUtc,
            Source = operation.Source
        };
    }

    public static StandaloneTripHistoryEntry Cancelled(
        int tripId,
        TripOperation operation,
        int? attemptNumber,
        string reason,
        WorkRecordStatus previousStatus,
        WorkRecordStatus newStatus)
    {
        ValidateOperation(operation);
        ValidateReason(reason);

        return new StandaloneTripHistoryEntry
        {
            TripId = tripId,
            EventType = StandaloneTripEventType.Cancelled,
            AttemptNumber = attemptNumber,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = operation.UserId,
            ChangedByUserName = operation.UserName,
            ChangedAtUtc = operation.AtUtc,
            Source = operation.Source
        };
    }

    public static StandaloneTripHistoryEntry Retried(
        int tripId,
        TripOperation operation,
        int attemptNumber,
        WorkRecordStatus previousStatus,
        WorkRecordStatus newStatus)
    {
        ValidateOperation(operation);

        return new StandaloneTripHistoryEntry
        {
            TripId = tripId,
            EventType = StandaloneTripEventType.Retried,
            AttemptNumber = attemptNumber,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = operation.UserId,
            ChangedByUserName = operation.UserName,
            ChangedAtUtc = operation.AtUtc,
            Source = operation.Source
        };
    }

    public static StandaloneTripHistoryEntry Reassigned(
        int tripId,
        TripOperation operation,
        string reason,
        int previousDeliverymanId,
        int newDeliverymanId,
        string previousDeliverymanName,
        string newDeliverymanName,
        long previousTripNumber,
        long newTripNumber)
    {
        ValidateOperation(operation);
        ValidateReason(reason);

        if (previousDeliverymanId <= 0 || newDeliverymanId <= 0)
            throw new ArgumentException("معرف المندوب يجب أن يكون أكبر من صفر.");

        if (string.IsNullOrWhiteSpace(previousDeliverymanName) || previousDeliverymanName.Length > 100)
            throw new ArgumentException("اسم المندوب السابق مطلوب.");

        if (string.IsNullOrWhiteSpace(newDeliverymanName) || newDeliverymanName.Length > 100)
            throw new ArgumentException("اسم المندوب الجديد مطلوب.");

        return new StandaloneTripHistoryEntry
        {
            TripId = tripId,
            EventType = StandaloneTripEventType.Reassigned,
            Reason = reason,
            PreviousDeliverymanId = previousDeliverymanId,
            NewDeliverymanId = newDeliverymanId,
            PreviousDeliverymanName = previousDeliverymanName,
            NewDeliverymanName = newDeliverymanName,
            PreviousTripNumber = previousTripNumber,
            NewTripNumber = newTripNumber,
            ChangedByUserId = operation.UserId,
            ChangedByUserName = operation.UserName,
            ChangedAtUtc = operation.AtUtc,
            Source = operation.Source
        };
    }

    public static StandaloneTripHistoryEntry Edited(
        int tripId,
        TripOperation operation,
        string? reason)
    {
        ValidateOperation(operation);

        if (reason != null)
        {
            var trimmed = reason.Trim();
            if (trimmed.Length > 200)
                throw new ArgumentException("ملاحظة التعديل يجب ألا تتجاوز 200 حرف.");
            reason = string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        return new StandaloneTripHistoryEntry
        {
            TripId = tripId,
            EventType = StandaloneTripEventType.Edited,
            Reason = reason,
            ChangedByUserId = operation.UserId,
            ChangedByUserName = operation.UserName,
            ChangedAtUtc = operation.AtUtc,
            Source = operation.Source
        };
    }

    private static void ValidateOperation(TripOperation operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        if (operation.UserId <= 0)
            throw new ArgumentException("معرف المستخدم يجب أن يكون أكبر من صفر.");

        if (string.IsNullOrWhiteSpace(operation.UserName) || operation.UserName.Length > 150)
            throw new ArgumentException("اسم المستخدم مطلوب.");

        if (operation.AtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("التوقيت يجب أن يكون UTC.");
    }

    private static void ValidateReason(string reason)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 200)
            throw new ArgumentException("السبب مطلوب ويجب ألا يتجاوز 200 حرف.");
    }

    private StandaloneTripHistoryEntry() { }
}
