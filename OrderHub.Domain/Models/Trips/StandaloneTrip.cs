using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models.Trips;

public class StandaloneTrip : ModelBase
{
    // Identity & Ownership
    public int DeliverymanId { get; private set; }
    public Deliveryman Deliveryman { get; private set; } = null!;

    // Core Fields
    public string Title { get; private set; } = null!;
    public string Description { get; private set; }
    public TripParty From { get; private set; } = null!;
    public TripParty To { get; private set; } = null!;
    public DateTime? ScheduledAtUtc { get; private set; }

    // Status & Timing
    public WorkRecordStatus Status { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime StatusChangedAtUtc { get; private set; }

    // Numbering
    public long TripNumber { get; private set; }
    public int SequenceYear { get; private set; }
    public int SequenceMonth { get; private set; }

    // Concurrency
    public int Version { get; private set; }

    // Collections
    private readonly List<StandaloneTripAttempt> _attempts = new();
    private readonly List<StandaloneTripHistoryEntry> _history = new();

    public IReadOnlyCollection<StandaloneTripAttempt> Attempts => _attempts.AsReadOnly();
    public IReadOnlyCollection<StandaloneTripHistoryEntry> History => _history.AsReadOnly();

    // Derived Properties
    public bool IsNew => Status == WorkRecordStatus.Pending 
                         && StartedAtUtc == null 
                         && _attempts.Count == 0;

    public StandaloneTripAttempt OpenAttempt => 
        _attempts.SingleOrDefault(a => a.Status == WorkRecordStatus.Pending);

    public int? LatestAttemptNumber => 
        _attempts.Count == 0 ? null : _attempts.Max(a => a.AttemptNumber);

    // Factory Method
    public static StandaloneTrip Create(
        int deliverymanId,
        string title,
        string description,
        TripParty from,
        TripParty to,
        DateTimeOffset? scheduledAtUtc,
        TripOperation operation)
    {
        // Validation
        if (deliverymanId <= 0) 
            throw new InvalidOperationException("معرف المندوب مطلوب.");
        
        var trimmedTitle = title?.Trim();
        if (string.IsNullOrEmpty(trimmedTitle) || trimmedTitle.Length > 150)
            throw new InvalidOperationException("العنوان مطلوب ويجب ألا يتجاوز 150 حرفاً.");

        var trimmedDesc = description?.Trim();
        if (!string.IsNullOrEmpty(trimmedDesc) && trimmedDesc.Length > 1000)
            throw new InvalidOperationException("الوصف يجب ألا يتجاوز 1000 حرف.");

        // Create trip
        var trip = new StandaloneTrip
        {
            DeliverymanId = deliverymanId,
            Title = trimmedTitle,
            Description = string.IsNullOrWhiteSpace(trimmedDesc) ? null : trimmedDesc,
            From = from ?? throw new ArgumentNullException(nameof(from)),
            To = to ?? throw new ArgumentNullException(nameof(to)),
            ScheduledAtUtc = scheduledAtUtc?.UtcDateTime,
            Status = WorkRecordStatus.Pending,
            StartedAtUtc = null,
            StatusChangedAtUtc = operation.AtUtc,
            Version = 1,
            TripNumber = 0,
            SequenceYear = 0,
            SequenceMonth = 0
        };

        // Add creation history
        trip._history.Add(StandaloneTripHistoryEntry.Created(
            trip.Id,
            operation,
            previousStatus: null,
            newStatus: WorkRecordStatus.Pending
        ));

        return trip;
    }

    // Numbering
    public void AssignNumber(long tripNumber, int sequenceYear, int sequenceMonth)
    {
        if (TripNumber != 0)
            throw new InvalidOperationException("تم تعيين رقم المشوار بالفعل.");
        
        if (tripNumber <= 0 || sequenceYear <= 0 || sequenceMonth < 1 || sequenceMonth > 12)
            throw new ArgumentException("رقم المشوار أو الفترة غير صالحة.");

        TripNumber = tripNumber;
        SequenceYear = sequenceYear;
        SequenceMonth = sequenceMonth;
    }

    // Lifecycle Methods
    public void Start(TripOperation operation)
    {
        if (Status != WorkRecordStatus.Pending)
            throw new InvalidOperationException("يمكن بدء المشوار في حالة الانتظار فقط.");

        if (_attempts.Count == 0)
        {
            // First start
            var attempt = StandaloneTripAttempt.Pending(1, operation.AtUtc);
            attempt.Start(operation.AtUtc);
            _attempts.Add(attempt);
            StartedAtUtc = operation.AtUtc;
        }
        else
        {
            // Retry start
            var open = OpenAttempt;
            if (open == null || open.StartedAtUtc != null)
                throw new InvalidOperationException("لا يمكن بدء المشوار في حالته الحالية.");
            
            open.Start(operation.AtUtc);
        }

        _history.Add(StandaloneTripHistoryEntry.AttemptStarted(
            Id,
            operation,
            LatestAttemptNumber ?? 1
        ));

        Version++;
    }

    public void Complete(TripOperation operation)
    {
        if (Status != WorkRecordStatus.Pending)
            throw new InvalidOperationException("يمكن إكمال المشوار في حالة الانتظار فقط.");

        var open = OpenAttempt;
        if (open == null || open.StartedAtUtc == null)
            throw new InvalidOperationException("لا يوجد محاولة جارية لإكمالها.");

        open.Complete(operation.AtUtc);
        Status = WorkRecordStatus.Completed;
        StatusChangedAtUtc = operation.AtUtc;

        _history.Add(StandaloneTripHistoryEntry.Completed(
            Id,
            operation,
            open.AttemptNumber,
            WorkRecordStatus.Pending,
            WorkRecordStatus.Completed
        ));

        Version++;
    }

    public void Fail(string reason, TripOperation operation)
    {
        if (Status != WorkRecordStatus.Pending)
            throw new InvalidOperationException("يمكن تفشيل المشوار في حالة الانتظار فقط.");

        var trimmedReason = reason?.Trim();
        if (string.IsNullOrEmpty(trimmedReason) || trimmedReason.Length > 200)
            throw new InvalidOperationException("السبب مطلوب ويجب ألا يتجاوز 200 حرف.");

        var open = OpenAttempt;
        if (open == null || open.StartedAtUtc == null)
            throw new InvalidOperationException("لا يوجد محاولة جارية لتفشيلها.");

        open.Fail(trimmedReason, operation.AtUtc);
        Status = WorkRecordStatus.Failed;
        StatusChangedAtUtc = operation.AtUtc;

        _history.Add(StandaloneTripHistoryEntry.Failed(
            Id,
            operation,
            open.AttemptNumber,
            trimmedReason,
            WorkRecordStatus.Pending,
            WorkRecordStatus.Failed
        ));

        Version++;
    }

    public void Cancel(string reason, TripOperation operation)
    {
        if (Status != WorkRecordStatus.Pending && Status != WorkRecordStatus.Failed)
            throw new InvalidOperationException("يمكن إلغاء المشوار في حالة الانتظار أو الفشل فقط.");

        var trimmedReason = reason?.Trim();
        if (string.IsNullOrEmpty(trimmedReason) || trimmedReason.Length > 200)
            throw new InvalidOperationException("السبب مطلوب ويجب ألا يتجاوز 200 حرف.");

        var previousStatus = Status;
        var open = OpenAttempt;
        
        if (open != null)
        {
            open.Cancel(trimmedReason, operation.AtUtc);
        }

        Status = WorkRecordStatus.Cancelled;
        StatusChangedAtUtc = operation.AtUtc;

        _history.Add(StandaloneTripHistoryEntry.Cancelled(
            Id,
            operation,
            open?.AttemptNumber,
            trimmedReason,
            previousStatus,
            WorkRecordStatus.Cancelled
        ));

        Version++;
    }

    public void Retry(TripOperation operation)
    {
        if (Status != WorkRecordStatus.Failed)
            throw new InvalidOperationException("يمكن إعادة المحاولة لمشوار فاشل فقط.");

        if (OpenAttempt != null)
            throw new InvalidOperationException("يوجد محاولة مفتوحة بالفعل.");

        var nextAttemptNumber = (_attempts.Max(a => a.AttemptNumber)) + 1;
        var attempt = StandaloneTripAttempt.Pending(nextAttemptNumber, operation.AtUtc);
        _attempts.Add(attempt);

        Status = WorkRecordStatus.Pending;
        StatusChangedAtUtc = operation.AtUtc;

        _history.Add(StandaloneTripHistoryEntry.Retried(
            Id,
            operation,
            nextAttemptNumber,
            WorkRecordStatus.Failed,
            WorkRecordStatus.Pending
        ));

        Version++;
    }

    public void Update(
        string title,
        string description,
        TripParty from,
        TripParty to,
        DateTimeOffset? scheduledAtUtc,
        int deliverymanId,
        string newDeliverymanName,
        long? newTripNumber,
        string reassignReason,
        TripOperation operation)
    {
        if (!IsNew)
            throw new InvalidOperationException("لا يمكن تعديل المشوار بعد بدء المحاولة الأولى.");

        // Validate fields
        var trimmedTitle = title?.Trim();
        if (string.IsNullOrEmpty(trimmedTitle) || trimmedTitle.Length > 150)
            throw new InvalidOperationException("العنوان مطلوب ويجب ألا يتجاوز 150 حرفاً.");

        var trimmedDesc = description?.Trim();
        if (!string.IsNullOrEmpty(trimmedDesc) && trimmedDesc.Length > 1000)
            throw new InvalidOperationException("الوصف يجب ألا يتجاوز 1000 حرف.");

        // Check reassignment
        if (deliverymanId == DeliverymanId)
        {
            // Pure edit; reassignReason carries the optional edit note in this path.
            if (newDeliverymanName != null || newTripNumber != null)
                throw new InvalidOperationException("المندوب الجديد مطابق للمندوب الحالي.");

            Title = trimmedTitle;
            Description = string.IsNullOrWhiteSpace(trimmedDesc) ? null : trimmedDesc;
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            ScheduledAtUtc = scheduledAtUtc?.UtcDateTime;

            _history.Add(StandaloneTripHistoryEntry.Edited(
                Id,
                operation,
                reassignReason // Can be null for pure edit
            ));
        }
        else
        {
            // Reassignment
            var trimmedReason = reassignReason?.Trim();
            if (string.IsNullOrEmpty(trimmedReason) || trimmedReason.Length > 200)
                throw new InvalidOperationException("سبب إعادة الإسناد مطلوب ويجب ألا يتجاوز 200 حرف.");

            if (string.IsNullOrWhiteSpace(newDeliverymanName) || newDeliverymanName.Length > 100)
                throw new InvalidOperationException("اسم المندوب الجديد مطلوب.");

            if (!newTripNumber.HasValue || newTripNumber.Value <= 0)
                throw new InvalidOperationException("رقم المشوار الجديد مطلوب.");

            var oldId = DeliverymanId;
            var oldName = Deliveryman?.Name.Value ?? "غير معروف";
            var oldNumber = TripNumber;

            // Apply changes
            Title = trimmedTitle;
            Description = string.IsNullOrWhiteSpace(trimmedDesc) ? null : trimmedDesc;
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            ScheduledAtUtc = scheduledAtUtc?.UtcDateTime;
            DeliverymanId = deliverymanId;
            TripNumber = newTripNumber.Value;

            _history.Add(StandaloneTripHistoryEntry.Reassigned(
                Id,
                operation,
                trimmedReason,
                oldId,
                deliverymanId,
                oldName,
                newDeliverymanName,
                oldNumber,
                newTripNumber.Value
            ));
        }

        Version++;
    }

    public TripDisplayState GetDisplayState()
    {
        if (Status == WorkRecordStatus.Completed) return TripDisplayState.Completed;
        if (Status == WorkRecordStatus.Cancelled) return TripDisplayState.Cancelled;
        if (Status == WorkRecordStatus.Failed) return TripDisplayState.Failed;

        if (Status == WorkRecordStatus.Pending)
        {
            if (StartedAtUtc == null) return TripDisplayState.New;
            
            var open = OpenAttempt;
            if (open != null && open.StartedAtUtc == null)
                return TripDisplayState.AwaitingRetryStart;
            
            return TripDisplayState.InProgress;
        }

        return TripDisplayState.New;
    }

    // Private constructor for EF
    private StandaloneTrip() { }
}
