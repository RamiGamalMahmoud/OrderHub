using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Domain.Models.Trips;

public class StandaloneTripAttempt
{
    public int Id { get; private set; }
    public int TripId { get; private set; }
    public int AttemptNumber { get; private set; }
    public WorkRecordStatus Status { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static StandaloneTripAttempt Pending(int attemptNumber, DateTime createdAtUtc)
    {
        if (attemptNumber <= 0)
            throw new ArgumentException("رقم المحاولة يجب أن يكون أكبر من صفر.");

        return new StandaloneTripAttempt
        {
            AttemptNumber = attemptNumber,
            Status = WorkRecordStatus.Pending,
            StartedAtUtc = null,
            EndedAtUtc = null,
            Reason = null,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void Start(DateTime atUtc)
    {
        if (Status != WorkRecordStatus.Pending)
            throw new InvalidOperationException("يمكن بدء محاولة في حالة الانتظار فقط.");

        if (StartedAtUtc != null)
            throw new InvalidOperationException("المحاولة قد بدأت بالفعل.");

        StartedAtUtc = atUtc;
    }

    public void Complete(DateTime atUtc)
    {
        if (Status != WorkRecordStatus.Pending)
            throw new InvalidOperationException("يمكن إكمال محاولة في حالة الانتظار فقط.");

        if (StartedAtUtc == null)
            throw new InvalidOperationException("يجب بدء المحاولة قبل إكمالها.");

        Status = WorkRecordStatus.Completed;
        EndedAtUtc = atUtc;
    }

    public void Fail(string reason, DateTime atUtc)
    {
        if (Status != WorkRecordStatus.Pending)
            throw new InvalidOperationException("يمكن تفشيل محاولة في حالة الانتظار فقط.");

        if (StartedAtUtc == null)
            throw new InvalidOperationException("يجب بدء المحاولة قبل تفشيلها.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("السبب مطلوب.");

        Status = WorkRecordStatus.Failed;
        EndedAtUtc = atUtc;
        Reason = reason;
    }

    public void Cancel(string reason, DateTime atUtc)
    {
        if (Status != WorkRecordStatus.Pending)
            throw new InvalidOperationException("يمكن إلغاء محاولة في حالة الانتظار فقط.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("السبب مطلوب.");

        Status = WorkRecordStatus.Cancelled;
        EndedAtUtc = atUtc;
        Reason = reason;
    }

    private StandaloneTripAttempt() { }
}
