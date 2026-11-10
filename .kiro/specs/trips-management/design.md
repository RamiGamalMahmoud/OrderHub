# Technical Design Document: Standalone Trips Management

## Document Information

- **Feature**: Standalone Trips Management (إدارة المشاوير المستقلة)
- **Version**: 1.0
- **Date**: 2024-11-09
- **Status**: Design Phase
- **Related Documents**: 
  - Requirements: `.kiro/specs/trips-management/requirements.md`
  - Specification: `plans/TRIPS-MANAGEMENT-SPEC.md`

## 1. High-Level Architecture

### 1.1 Architectural Overview

The Standalone Trips feature follows the existing vertical slice CQRS architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                         Web Layer                            │
│  - Razor Components (Pages/Dialogs)                         │
│  - Client-side validation                                   │
│  - Permission-gated UI rendering                            │
└────────────────┬────────────────────────────────────────────┘
                 │ IRequestExecutor
┌────────────────▼────────────────────────────────────────────┐
│                     Application Layer                        │
│  - Commands (Create, Update, Lifecycle)                     │
│  - Queries (List, Details, Lookups)                         │
│  - Validators                                               │
│  - Result<T> pattern                                        │
└────────────────┬────────────────────────────────────────────┘
                 │ Repository + Read Models
┌────────────────▼────────────────────────────────────────────┐
│                   Infrastructure Layer                       │
│  - EF Core Configurations                                   │
│  - Repositories                                             │
│  - Read Model Projections                                   │
│  - Number Sequence Integration                              │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│                      Domain Layer                            │
│  - StandaloneTrip Aggregate                                 │
│  - TripParty Value Object                                   │
│  - Attempt & History Entities                               │
│  - Lifecycle Rules                                          │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 Key Design Principles

1. **Isolation**: No changes to existing `DeliveryTrip`, `Order`, or `WorkRecordLifecycle`
2. **Explicit Lifecycle**: Manual state transitions through explicit commands
3. **Immutable History**: Append-only audit trail with actor tracking
4. **Optimistic Concurrency**: Version-based conflict detection
5. **Permission-First**: All operations gated by `Trips.Read` or `Trips.Write`
6. **Snapshot Pattern**: Party data frozen after trip start

## 2. Domain Model Design

### 2.1 Aggregate Root: StandaloneTrip

```csharp
public class StandaloneTrip : ModelBase
{
    // Identity & Ownership
    public int Id { get; private set; }
    public int DeliverymanId { get; private set; }
    public Deliveryman Deliveryman { get; private set; } = null!;

    // Core Fields
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
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

    public StandaloneTripAttempt? OpenAttempt => 
        _attempts.SingleOrDefault(a => a.Status == WorkRecordStatus.Pending);

    public int? LatestAttemptNumber => 
        _attempts.Count == 0 ? null : _attempts.Max(a => a.AttemptNumber);

    // Factory Method
    public static StandaloneTrip Create(
        int deliverymanId,
        string? title,
        string? description,
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

    public void Fail(string? reason, TripOperation operation)
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

    public void Cancel(string? reason, TripOperation operation)
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
        string? title,
        string? description,
        TripParty from,
        TripParty to,
        DateTimeOffset? scheduledAtUtc,
        int deliverymanId,
        string? newDeliverymanName,
        long? newTripNumber,
        string? reassignReason,
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
            // Pure edit
            if (newDeliverymanName != null || newTripNumber != null || reassignReason != null)
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
```

### 2.2 Value Object: TripParty

```csharp
public class TripParty
{
    public TripPartyType PartyType { get; private set; }
    public int? PartyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? Location { get; private set; }

    // Factory Methods
    public static TripParty FromClient(Client client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));

        return new TripParty
        {
            PartyType = TripPartyType.Client,
            PartyId = client.Id,
            Name = Normalize(client.Name.Value) ?? throw new InvalidOperationException("اسم العميل مطلوب."),
            Phone = NormalizeToNull(client.Phone?.Number.FullNumber),
            Address = NormalizeToNull(client.Address?.FullAddress),
            Location = NormalizeToNull(client.Location)
        };
    }

    public static TripParty FromSupplier(Supplier supplier)
    {
        if (supplier == null) throw new ArgumentNullException(nameof(supplier));

        return new TripParty
        {
            PartyType = TripPartyType.Supplier,
            PartyId = supplier.Id,
            Name = Normalize(supplier.Name.Value) ?? throw new InvalidOperationException("اسم المورد مطلوب."),
            Phone = NormalizeToNull(supplier.Phone?.Number.FullNumber),
            Address = NormalizeToNull(supplier.Address?.FullAddress),
            Location = null
        };
    }

    public static TripParty FromShippingCarrier(ShippingCarrier carrier)
    {
        if (carrier == null) throw new ArgumentNullException(nameof(carrier));

        return new TripParty
        {
            PartyType = TripPartyType.ShippingCarrier,
            PartyId = carrier.Id,
            Name = Normalize(carrier.Name.Value) ?? throw new InvalidOperationException("اسم شركة الشحن مطلوب."),
            Phone = NormalizeToNull(carrier.Phone?.Number.FullNumber),
            Address = NormalizeToNull(carrier.Address?.FullAddress),
            Location = null
        };
    }

    public static TripParty External(
        string? name,
        string? phone,
        string? address,
        string? location)
    {
        var normalizedName = Normalize(name);
        var normalizedPhone = Normalize(phone);
        var normalizedAddress = Normalize(address);

        if (string.IsNullOrEmpty(normalizedName) || normalizedName.Length > 100)
            throw new InvalidOperationException("اسم الطرف الخارجي مطلوب ويجب ألا يتجاوز 100 حرف.");

        if (string.IsNullOrEmpty(normalizedPhone))
            throw new InvalidOperationException("رقم الهاتف مطلوب للطرف الخارجي.");

        if (normalizedPhone.Length < 6 || normalizedPhone.Length > 22)
            throw new InvalidOperationException("رقم الهاتف يجب أن يكون بين 6 و 22 حرفاً.");

        var digitCount = normalizedPhone.Count(char.IsDigit);
        if (digitCount < 6)
            throw new InvalidOperationException("رقم الهاتف يجب أن يحتوي على 6 أرقام على الأقل.");

        if (string.IsNullOrEmpty(normalizedAddress) || normalizedAddress.Length > 255)
            throw new InvalidOperationException("العنوان مطلوب ويجب ألا يتجاوز 255 حرفاً.");

        var normalizedLocation = NormalizeToNull(location);
        if (normalizedLocation != null && normalizedLocation.Length > 255)
            throw new InvalidOperationException("الموقع يجب ألا يتجاوز 255 حرفاً.");

        return new TripParty
        {
            PartyType = TripPartyType.External,
            PartyId = null,
            Name = normalizedName,
            Phone = normalizedPhone,
            Address = normalizedAddress,
            Location = normalizedLocation
        };
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? NormalizeToNull(string? value)
    {
        var normalized = Normalize(value);
        // Map empty strings to null (from supplier/carrier readers)
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    // Private constructor for EF
    private TripParty() { }
}
```

### 2.3 Entity: StandaloneTripAttempt

```csharp
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
```

### 2.4 Entity: StandaloneTripHistoryEntry

```csharp
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
```

### 2.5 Supporting Types

```csharp
public record TripOperation(
    int UserId,
    string UserName,
    DateTime AtUtc,
    string Source = "Web");

public enum TripPartyType
{
    Client,
    Supplier,
    ShippingCarrier,
    External
}

public enum StandaloneTripEventType
{
    Created,
    AttemptStarted,
    Completed,
    Failed,
    Cancelled,
    Retried,
    Reassigned,
    Edited
}

public enum TripDisplayState
{
    New,
    InProgress,
    AwaitingRetryStart,
    Failed,
    Completed,
    Cancelled
}
```

## 3. Database Schema Design

### 3.1 Tables

#### standalone_trips

```sql
CREATE TABLE standalone_trips (
    id SERIAL PRIMARY KEY,
    deliveryman_id INTEGER NOT NULL,
    title VARCHAR(150) NOT NULL,
    description VARCHAR(1000),
    
    -- From party (owned type)
    from_party_type VARCHAR(30) NOT NULL,
    from_party_id INTEGER,
    from_name VARCHAR(100) NOT NULL,
    from_phone VARCHAR(30),
    from_address VARCHAR(255),
    from_location VARCHAR(255),
    
    -- To party (owned type)
    to_party_type VARCHAR(30) NOT NULL,
    to_party_id INTEGER,
    to_name VARCHAR(100) NOT NULL,
    to_phone VARCHAR(30),
    to_address VARCHAR(255),
    to_location VARCHAR(255),
    
    scheduled_at_utc TIMESTAMP WITH TIME ZONE,
    status VARCHAR(30) NOT NULL,
    started_at_utc TIMESTAMP WITH TIME ZONE,
    status_changed_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    
    trip_number BIGINT NOT NULL,
    sequence_year INTEGER NOT NULL,
    sequence_month INTEGER NOT NULL,
    
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    modified_at TIMESTAMP WITH TIME ZONE NOT NULL,
    
    CONSTRAINT fk_standalone_trips_deliverymen 
        FOREIGN KEY (deliveryman_id) 
        REFERENCES deliverymen(id) 
        ON DELETE RESTRICT,
    
    CONSTRAINT ck_standalone_trips_trip_number 
        CHECK (trip_number > 0),
    
    CONSTRAINT ck_standalone_trips_sequence_year 
        CHECK (sequence_year > 0),
    
    CONSTRAINT ck_standalone_trips_sequence_month 
        CHECK (sequence_month BETWEEN 1 AND 12),
    
    CONSTRAINT ck_standalone_trips_status 
        CHECK (status IN ('Pending', 'Completed', 'Cancelled', 'Failed')),
    
    CONSTRAINT ck_standalone_trips_external_party 
        CHECK (
            (from_party_type != 'External' OR (from_party_id IS NULL AND from_name IS NOT NULL AND from_phone IS NOT NULL AND from_address IS NOT NULL))
            AND
            (to_party_type != 'External' OR (to_party_id IS NULL AND to_name IS NOT NULL AND to_phone IS NOT NULL AND to_address IS NOT NULL))
        ),
    
    CONSTRAINT ck_standalone_trips_known_party 
        CHECK (
            (from_party_type = 'External' OR from_party_id > 0)
            AND
            (to_party_type = 'External' OR to_party_id > 0)
        ),
    
    CONSTRAINT ck_standalone_trips_title_length 
        CHECK (length(trim(title)) BETWEEN 1 AND 150)
);

-- Indexes
CREATE UNIQUE INDEX uq_standalone_trips_number 
    ON standalone_trips(deliveryman_id, sequence_year, sequence_month, trip_number);

CREATE INDEX ix_standalone_trips_deliveryman_created 
    ON standalone_trips(deliveryman_id, created_at);

CREATE INDEX ix_standalone_trips_from_party 
    ON standalone_trips(from_party_type, from_party_id);

CREATE INDEX ix_standalone_trips_to_party 
    ON standalone_trips(to_party_type, to_party_id);

CREATE INDEX ix_standalone_trips_status_scheduled 
    ON standalone_trips(status, scheduled_at_utc);

CREATE INDEX ix_standalone_trips_created 
    ON standalone_trips(created_at);
```

#### standalone_trip_attempts

```sql
CREATE TABLE standalone_trip_attempts (
    id SERIAL PRIMARY KEY,
    trip_id INTEGER NOT NULL,
    attempt_number INTEGER NOT NULL,
    status VARCHAR(30) NOT NULL,
    started_at_utc TIMESTAMP WITH TIME ZONE,
    ended_at_utc TIMESTAMP WITH TIME ZONE,
    reason VARCHAR(200),
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    
    CONSTRAINT fk_standalone_trip_attempts_trips 
        FOREIGN KEY (trip_id) 
        REFERENCES standalone_trips(id) 
        ON DELETE RESTRICT,
    
    CONSTRAINT ck_standalone_trip_attempts_number 
        CHECK (attempt_number > 0),
    
    CONSTRAINT ck_standalone_trip_attempts_status 
        CHECK (status IN ('Pending', 'Completed', 'Cancelled', 'Failed')),
    
    CONSTRAINT ck_standalone_trip_attempts_pending_no_end 
        CHECK (status != 'Pending' OR ended_at_utc IS NULL),
    
    CONSTRAINT ck_standalone_trip_attempts_terminal_has_end 
        CHECK (status = 'Pending' OR ended_at_utc IS NOT NULL),
    
    CONSTRAINT ck_standalone_trip_attempts_completed_started 
        CHECK (status NOT IN ('Completed', 'Failed') OR started_at_utc IS NOT NULL),
    
    CONSTRAINT ck_standalone_trip_attempts_reason 
        CHECK (
            (status NOT IN ('Failed', 'Cancelled')) 
            OR 
            (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)
        ),
    
    CONSTRAINT ck_standalone_trip_attempts_timing 
        CHECK (
            ended_at_utc IS NULL 
            OR started_at_utc IS NULL 
            OR ended_at_utc >= started_at_utc
        )
);

-- Indexes
CREATE UNIQUE INDEX uq_standalone_trip_attempts_number 
    ON standalone_trip_attempts(trip_id, attempt_number);

CREATE UNIQUE INDEX uq_standalone_trip_attempts_open 
    ON standalone_trip_attempts(trip_id) 
    WHERE status = 'Pending';

CREATE INDEX ix_standalone_trip_attempts_status 
    ON standalone_trip_attempts(trip_id, status);
```

#### standalone_trip_histories

```sql
CREATE TABLE standalone_trip_histories (
    id SERIAL PRIMARY KEY,
    trip_id INTEGER NOT NULL,
    event_type VARCHAR(30) NOT NULL,
    attempt_number INTEGER,
    previous_status VARCHAR(30),
    new_status VARCHAR(30),
    previous_deliveryman_id INTEGER,
    new_deliveryman_id INTEGER,
    previous_deliveryman_name VARCHAR(100),
    new_deliveryman_name VARCHAR(100),
    previous_trip_number BIGINT,
    new_trip_number BIGINT,
    reason VARCHAR(200),
    changed_by_user_id INTEGER NOT NULL,
    changed_by_user_name VARCHAR(150) NOT NULL,
    changed_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    source VARCHAR(30) NOT NULL,
    
    CONSTRAINT fk_standalone_trip_histories_trips 
        FOREIGN KEY (trip_id) 
        REFERENCES standalone_trips(id) 
        ON DELETE RESTRICT,
    
    CONSTRAINT fk_standalone_trip_histories_users 
        FOREIGN KEY (changed_by_user_id) 
        REFERENCES users(id) 
        ON DELETE RESTRICT,
    
    CONSTRAINT ck_standalone_trip_histories_event_type 
        CHECK (event_type IN (
            'Created', 'AttemptStarted', 'Completed', 'Failed', 
            'Cancelled', 'Retried', 'Reassigned', 'Edited'
        )),
    
    CONSTRAINT ck_standalone_trip_histories_reason 
        CHECK (
            (event_type NOT IN ('Failed', 'Cancelled', 'Reassigned')) 
            OR 
            (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)
        ),
    
    CONSTRAINT ck_standalone_trip_histories_reassignment 
        CHECK (
            (event_type != 'Reassigned') 
            OR 
            (
                previous_deliveryman_id > 0 
                AND new_deliveryman_id > 0
                AND previous_deliveryman_name IS NOT NULL
                AND new_deliveryman_name IS NOT NULL
                AND previous_trip_number > 0
                AND new_trip_number > 0
            )
        )
);

-- Indexes
CREATE INDEX ix_standalone_trip_histories_trip 
    ON standalone_trip_histories(trip_id, id);

CREATE INDEX ix_standalone_trip_histories_prev_deliveryman 
    ON standalone_trip_histories(previous_deliveryman_id);

CREATE INDEX ix_standalone_trip_histories_new_deliveryman 
    ON standalone_trip_histories(new_deliveryman_id);
```

### 3.2 EF Core Configurations

```csharp
public class StandaloneTripConfiguration : IEntityTypeConfiguration<StandaloneTrip>
{
    public void Configure(EntityTypeBuilder<StandaloneTrip> builder)
    {
        builder.ToTable("standalone_trips");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.DeliverymanId)
            .HasColumnName("deliveryman_id")
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        // From party (owned type)
        builder.OwnsOne(t => t.From, from =>
        {
            from.Property(p => p.PartyType)
                .HasColumnName("from_party_type")
                .HasMaxLength(30)
                .HasConversion<string>()
                .IsRequired();

            from.Property(p => p.PartyId)
                .HasColumnName("from_party_id");

            from.Property(p => p.Name)
                .HasColumnName("from_name")
                .HasMaxLength(100)
                .IsRequired();

            from.Property(p => p.Phone)
                .HasColumnName("from_phone")
                .HasMaxLength(30);

            from.Property(p => p.Address)
                .HasColumnName("from_address")
                .HasMaxLength(255);

            from.Property(p => p.Location)
                .HasColumnName("from_location")
                .HasMaxLength(255);
        });

        // To party (owned type)
        builder.OwnsOne(t => t.To, to =>
        {
            to.Property(p => p.PartyType)
                .HasColumnName("to_party_type")
                .HasMaxLength(30)
                .HasConversion<string>()
                .IsRequired();

            to.Property(p => p.PartyId)
                .HasColumnName("to_party_id");

            to.Property(p => p.Name)
                .HasColumnName("to_name")
                .HasMaxLength(100)
                .IsRequired();

            to.Property(p => p.Phone)
                .HasColumnName("to_phone")
                .HasMaxLength(30);

            to.Property(p => p.Address)
                .HasColumnName("to_address")
                .HasMaxLength(255);

            to.Property(p => p.Location)
                .HasColumnName("to_location")
                .HasMaxLength(255);
        });

        builder.Property(t => t.ScheduledAtUtc)
            .HasColumnName("scheduled_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.StatusChangedAtUtc)
            .HasColumnName("status_changed_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.TripNumber)
            .HasColumnName("trip_number")
            .IsRequired();

        builder.Property(t => t.SequenceYear)
            .HasColumnName("sequence_year")
            .IsRequired();

        builder.Property(t => t.SequenceMonth)
            .HasColumnName("sequence_month")
            .IsRequired();

        builder.Property(t => t.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.ModifiedAt)
            .HasColumnName("modified_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relationships
        builder.HasOne(t => t.Deliveryman)
            .WithMany()
            .HasForeignKey(t => t.DeliverymanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Attempts)
            .WithOne()
            .HasForeignKey("TripId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.History)
            .WithOne()
            .HasForeignKey("TripId")
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(t => new { t.DeliverymanId, t.SequenceYear, t.SequenceMonth, t.TripNumber })
            .IsUnique()
            .HasDatabaseName("uq_standalone_trips_number");

        builder.HasIndex(t => new { t.DeliverymanId, t.CreatedAt })
            .HasDatabaseName("ix_standalone_trips_deliveryman_created");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("ix_standalone_trips_created");
    }
}
```

## 4. Application Layer Design

### 4.1 Command/Query Structure

```
OrderHub.Application/Features/Trips/
├── Contracts/
│   ├── TripPartyInput.cs
│   ├── TripInputValidation.cs
│   ├── TripPartySnapshotFactory.cs
│   ├── TripOperationFactory.cs
│   ├── IStandaloneTripRepository.cs
│   ├── ITripReader.cs
│   ├── TripListItem.cs
│   ├── TripDetails.cs
│   ├── TripEditorData.cs
│   ├── TripSearchCriteria.cs
│   ├── TripLookupKind.cs
│   ├── TripLookupItem.cs
│   ├── DeliverymanTripsSummary.cs
│   ├── DeliverymanTripListItem.cs
│   └── OrderBoundTripListItem.cs
├── CreateTrip/
│   ├── CreateTripCommand.cs
│   ├── CreateTripCommandHandler.cs
│   └── CreateTripReceipt.cs
├── UpdateTrip/
│   ├── UpdateTripCommand.cs
│   └── UpdateTripCommandHandler.cs
├── StartTrip/
│   ├── StartTripCommand.cs
│   └── StartTripCommandHandler.cs
├── CompleteTrip/
│   ├── CompleteTripCommand.cs
│   └── CompleteTripCommandHandler.cs
├── FailTrip/
│   ├── FailTripCommand.cs
│   └── FailTripCommandHandler.cs
├── CancelTrip/
│   ├── CancelTripCommand.cs
│   └── CancelTripCommandHandler.cs
├── RetryTrip/
│   ├── RetryTripCommand.cs
│   └── RetryTripCommandHandler.cs
├── GetTripsPage/
│   ├── GetTripsPageQuery.cs
│   └── GetTripsPageQueryHandler.cs
├── GetTripDetails/
│   ├── GetTripDetailsQuery.cs
│   └── GetTripDetailsQueryHandler.cs
├── GetTripEditor/
│   ├── GetTripEditorQuery.cs
│   └── GetTripEditorQueryHandler.cs
├── GetTripLookups/
│   ├── GetTripLookupsQuery.cs
│   └── GetTripLookupsQueryHandler.cs
├── GetDeliverymanTripsSummary/
│   ├── GetDeliverymanTripsSummaryQuery.cs
│   └── GetDeliverymanTripsSummaryQueryHandler.cs
├── GetDeliverymanStandaloneTrips/
│   ├── GetDeliverymanStandaloneTripsQuery.cs
│   └── GetDeliverymanStandaloneTripsQueryHandler.cs
└── GetDeliverymanOrderBoundTrips/
    ├── GetDeliverymanOrderBoundTripsQuery.cs
    └── GetDeliverymanOrderBoundTripsQueryHandler.cs
```

### 4.2 Key Commands

#### CreateTripCommand

```csharp
[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record CreateTripCommand(
    int DeliverymanId,
    string? Title,
    string? Description,
    TripPartyInput From,
    TripPartyInput To,
    DateTimeOffset? ScheduledAtUtc
) : IRequest<Result<CreateTripReceipt>>;

public record CreateTripReceipt(
    int Id,
    int Version,
    WorkRecordStatus Status,
    long TripNumber,
    int SequenceYear,
    int SequenceMonth
);
```

#### UpdateTripCommand

```csharp
[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record UpdateTripCommand(
    int Id,
    int ExpectedVersion,
    int DeliverymanId,
    string? Title,
    string? Description,
    TripPartyInput From,
    TripPartyInput To,
    DateTimeOffset? ScheduledAtUtc,
    string? EditNote,
    string? ReassignReason
) : IRequest<Result<int>>; // Returns new version
```

#### Lifecycle Commands

```csharp
[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record StartTripCommand(
    int Id,
    int ExpectedVersion
) : IRequest<Result<int>>;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record CompleteTripCommand(
    int Id,
    int ExpectedVersion
) : IRequest<Result<int>>;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record FailTripCommand(
    int Id,
    int ExpectedVersion,
    string? Reason
) : IRequest<Result<int>>;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record CancelTripCommand(
    int Id,
    int ExpectedVersion,
    string? Reason
) : IRequest<Result<int>>;

[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]
public record RetryTripCommand(
    int Id,
    int ExpectedVersion
) : IRequest<Result<int>>;
```

### 4.3 Key Queries

#### GetTripsPageQuery

```csharp
[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read)]
public record GetTripsPageQuery(
    TripSearchCriteria Criteria,
    int Page,
    int PageSize
) : IRequest<PagedResult<TripListItem>>;

public record TripSearchCriteria(
    string? Search,
    IReadOnlyCollection<TripDisplayState> DisplayStates,
    int? DeliverymanId,
    TripPartyType? PartyType,
    DateOnly? ScheduledFrom,
    DateOnly? ScheduledTo,
    DateOnly? CreatedFrom,
    DateOnly? CreatedTo
);

public record TripListItem(
    int Id,
    long TripNumber,
    int SequenceYear,
    int SequenceMonth,
    string Title,
    int DeliverymanId,
    string DeliverymanName,
    TripPartyType FromType,
    string FromName,
    TripPartyType ToType,
    string ToName,
    DateTime? ScheduledAtUtc,
    WorkRecordStatus Status,
    TripDisplayState DisplayState,
    int? CurrentAttemptNumber,
    int AttemptCount,
    bool HasFailedAttempt,
    DateTime CreatedAt,
    int Version,
    bool IsEditable
);
```

#### GetTripDetailsQuery

```csharp
[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read)]
public record GetTripDetailsQuery(
    int Id,
    int HistoryPage = 1,
    int HistoryPageSize = 10
) : IRequest<Result<TripDetails>>;

public record TripDetails(
    int Id,
    long TripNumber,
    int SequenceYear,
    int SequenceMonth,
    string Title,
    string? Description,
    int DeliverymanId,
    string DeliverymanName,
    TripPartyData From,
    TripPartyData To,
    DateTime? ScheduledAtUtc,
    DateTime CreatedAt,
    DateTime StatusChangedAtUtc,
    WorkRecordStatus Status,
    TripDisplayState DisplayState,
    int? CurrentAttemptNumber,
    int AttemptCount,
    bool HasFailedAttempt,
    int Version,
    bool IsEditable,
    IReadOnlyList<TripAttemptData> Attempts,
    PagedResult<TripHistoryEntryData> History
);

public record TripPartyData(
    TripPartyType PartyType,
    int? PartyId,
    string Name,
    string? Phone,
    string? Address,
    string? Location
);

public record TripAttemptData(
    int AttemptNumber,
    WorkRecordStatus Status,
    DateTime? StartedAtUtc,
    DateTime? EndedAtUtc,
    string? Reason,
    DateTime CreatedAtUtc
);

public record TripHistoryEntryData(
    int Id,
    StandaloneTripEventType EventType,
    int? AttemptNumber,
    WorkRecordStatus? PreviousStatus,
    WorkRecordStatus? NewStatus,
    int? PreviousDeliverymanId,
    int? NewDeliverymanId,
    string? PreviousDeliverymanName,
    string? NewDeliverymanName,
    long? PreviousTripNumber,
    long? NewTripNumber,
    string? Reason,
    int ChangedByUserId,
    string ChangedByUserName,
    DateTime ChangedAtUtc,
    string Source
);
```

### 4.4 Supporting Contracts

```csharp
public record TripPartyInput(
    TripPartyType PartyType,
    int? PartyId,
    string? Name,
    string? Phone,
    string? Address,
    string? Location
);

public enum TripLookupKind
{
    Deliveryman,
    Client,
    Supplier,
    ShippingCarrier
}

public record TripLookupItem(
    int Id,
    string Name,
    string? Detail
);

public record DeliverymanTripsSummary(
    int OpenCount,
    int FailedCount,
    int CompletedCount,
    int CancelledCount,
    int TotalCount
);
```

### 4.5 Handler Pattern Example

```csharp
public class CreateTripCommandHandler : IRequestHandler<CreateTripCommand, Result<CreateTripReceipt>>
{
    private readonly IStandaloneTripRepository _repository;
    private readonly IDeliverymanRepository _deliverymanRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IShippingCarrierRepository _carrierRepository;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateTripCommandHandler(
        IStandaloneTripRepository repository,
        IDeliverymanRepository deliverymanRepository,
        IClientRepository clientRepository,
        ISupplierRepository supplierRepository,
        IShippingCarrierRepository carrierRepository,
        INumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _deliverymanRepository = deliverymanRepository;
        _clientRepository = clientRepository;
        _supplierRepository = supplierRepository;
        _carrierRepository = carrierRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreateTripReceipt>> Handle(
        CreateTripCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Build operation
        var operationResult = TripOperationFactory.Create(_currentUser, _timeProvider);
        if (!operationResult.IsSuccess)
            return Result<CreateTripReceipt>.Failure(operationResult.ErrorMessage, operationResult.ErrorCode);

        var operation = operationResult.Value;

        // 2. Validate inputs
        var validationResult = TripInputValidation.ValidateCreate(
            request.Title,
            request.Description,
            request.From,
            request.To);

        if (!validationResult.IsSuccess)
            return Result<CreateTripReceipt>.Failure(validationResult.ErrorMessage, "InvalidOperation");

        // 3. Resolve deliveryman
        var deliveryman = await _deliverymanRepository.FindAsync(
            request.DeliverymanId, 
            cancellationToken);

        if (deliveryman == null)
            return Result<CreateTripReceipt>.Failure("المندوب غير موجود.", "NotFound");

        // 4. Resolve party snapshots
        var fromResult = await TripPartySnapshotFactory.ResolveAsync(
            request.From,
            _clientRepository,
            _supplierRepository,
            _carrierRepository,
            _deliverymanRepository,
            cancellationToken);

        if (!fromResult.IsSuccess)
            return Result<CreateTripReceipt>.Failure(fromResult.ErrorMessage, "InvalidOperation");

        var toResult = await TripPartySnapshotFactory.ResolveAsync(
            request.To,
            _clientRepository,
            _supplierRepository,
            _carrierRepository,
            _deliverymanRepository,
            cancellationToken);

        if (!toResult.IsSuccess)
            return Result<CreateTripReceipt>.Failure(toResult.ErrorMessage, "InvalidOperation");

        // 5. Create trip (without number)
        var trip = StandaloneTrip.Create(
            request.DeliverymanId,
            request.Title,
            request.Description,
            fromResult.Value,
            toResult.Value,
            request.ScheduledAtUtc,
            operation);

        // 6. Set creation timestamp for numbering
        trip.UpdateCreationDate(_timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            // 7. Begin transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 8. Allocate number
            var createdAtOffset = new DateTimeOffset(
                DateTime.SpecifyKind(trip.CreatedAt, DateTimeKind.Utc));

            var allocation = await _numberSequenceService.NextMonthlyAsync(
                NumberSequenceKind.Deliveryman,
                request.DeliverymanId,
                createdAtOffset,
                cancellationToken);

            // 9. Assign number
            trip.AssignNumber(allocation.Number, allocation.Year, allocation.Month);

            // 10. Add and save
            _repository.Add(trip);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 11. Return receipt
            return Result<CreateTripReceipt>.Success(new CreateTripReceipt(
                trip.Id,
                trip.Version,
                trip.Status,
                trip.TripNumber,
                trip.SequenceYear,
                trip.SequenceMonth
            ));
        }
        catch (PersistenceConflictException)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<CreateTripReceipt>.Failure(
                "حدث تعارض أثناء الحفظ. يرجى المحاولة مرة أخرى.",
                "Conflict");
        }
        catch (DbUpdateConcurrencyException)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<CreateTripReceipt>.Failure(
                "حدث تعارض أثناء الحفظ. يرجى المحاولة مرة أخرى.",
                "Conflict");
        }
        catch (PersistenceException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<CreateTripReceipt>.Failure(
                $"خطأ في حفظ البيانات: {ex.Message}",
                "Persistence");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

## 5. UI Component Structure

### 5.1 Page Organization

```
OrderHub.Web/Components/Pages/Features/Trips/
├── _Imports.razor
├── Index.razor                          # /trips - List view
├── Index.razor.cs
├── Details.razor                        # /trips/{id} - Details view
├── Details.razor.cs
├── TripPresentation.cs                  # Status chips, action matrix
├── TripActionDialog.razor               # Reusable action confirmation
├── TripActionDialog.razor.cs
├── Editor/
│   ├── TripEditor.razor                 # /trips/create, /trips/{id}/edit
│   ├── TripEditor.razor.cs
│   ├── TripEditorState.cs               # Validation state
│   ├── TripPartyEditor.razor            # Party selector component
│   └── TripReassignDialog.razor         # Reassignment confirmation
```

### 5.2 Key Components

#### TripPresentation

```csharp
public static class TripPresentation
{
    // Status chip mapping
    public static (string Label, string Tone) GetStatusChip(TripDisplayState state)
    {
        return state switch
        {
            TripDisplayState.New => ("جديد", "slate"),
            TripDisplayState.InProgress => ("قيد التنفيذ", "brand"),
            TripDisplayState.AwaitingRetryStart => ("قيد التنفيذ — بانتظار البدء", "brand"),
            TripDisplayState.Failed => ("فشل", "rose"),
            TripDisplayState.Completed => ("مكتمل", "emerald"),
            TripDisplayState.Cancelled => ("ملغي", "slate"),
            _ => ("غير معروف", "slate")
        };
    }

    // Attempt chip mapping
    public static (string Label, string Tone) GetAttemptChip(
        WorkRecordStatus status,
        bool isStarted)
    {
        return (status, isStarted) switch
        {
            (WorkRecordStatus.Pending, false) => ("بانتظار البدء", "amber"),
            (WorkRecordStatus.Pending, true) => ("قيد التنفيذ", "brand"),
            (WorkRecordStatus.Completed, _) => ("مكتمل", "emerald"),
            (WorkRecordStatus.Failed, _) => ("فشل", "rose"),
            (WorkRecordStatus.Cancelled, _) => ("ملغي", "slate"),
            _ => ("غير معروف", "slate")
        };
    }

    // Action matrix
    public static IReadOnlyList<string> GetAllowedActions(
        TripDisplayState state,
        bool hasWritePermission)
    {
        if (!hasWritePermission)
            return Array.Empty<string>();

        return state switch
        {
            TripDisplayState.New => new[] { "Edit", "Start", "Cancel" },
            TripDisplayState.InProgress => new[] { "Complete", "Fail", "Cancel" },
            TripDisplayState.AwaitingRetryStart => new[] { "Start", "Cancel" },
            TripDisplayState.Failed => new[] { "Retry", "Cancel" },
            TripDisplayState.Completed => Array.Empty<string>(),
            TripDisplayState.Cancelled => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
    }
}
```

#### TripEditor Component Structure

```razor
@page "/trips/create"
@page "/trips/{TripId:int}/edit"
@attribute [PermissionAuthorize(PermissionFeature.Trips, PermissionLevel.Write)]

<PageScaffold Title="@PageTitle" Description="@PageDescription">
    <Actions>
        <button class="oh-btn oh-btn--secondary" @onclick="Cancel">
            إلغاء
        </button>
        <button class="oh-btn oh-btn--primary" 
                @onclick="Save" 
                disabled="@(_isBusy || !_state.IsValid)">
            @if (_isBusy)
            {
                <span>جاري الحفظ...</span>
            }
            else
            {
                <span>حفظ المشوار</span>
            }
        </button>
    </Actions>

    <ChildContent>
        <div class="space-y-4">
            <!-- Deliveryman Section -->
            <div class="rounded-xl border border-slate-200/90 bg-white p-4 shadow-xs">
                <MudAutocomplete @bind-Value="_state.DeliverymanId"
                                 Label="المندوب"
                                 SearchFunc="SearchDeliverymen"
                                 Required="true" />
            </div>

            <!-- Basic Info Section -->
            <div class="rounded-xl border border-slate-200/90 bg-white p-4 shadow-xs">
                <MudTextField @bind-Value="_state.Title"
                              Label="العنوان"
                              Required="true"
                              MaxLength="150" />
                
                <MudTextField @bind-Value="_state.Description"
                              Label="الوصف"
                              Lines="3"
                              MaxLength="1000" />
                
                <MudDatePicker @bind-Date="_state.ScheduledDate"
                               Label="التاريخ المجدول" />
            </div>

            <!-- From Party Section -->
            <TripPartyEditor @bind-PartyType="_state.FromType"
                           @bind-PartyId="_state.FromPartyId"
                           @bind-Name="_state.FromName"
                           @bind-Phone="_state.FromPhone"
                           @bind-Address="_state.FromAddress"
                           @bind-Location="_state.FromLocation"
                           Label="من" />

            <!-- To Party Section -->
            <TripPartyEditor @bind-PartyType="_state.ToType"
                           @bind-PartyId="_state.ToPartyId"
                           @bind-Name="_state.ToName"
                           @bind-Phone="_state.ToPhone"
                           @bind-Address="_state.ToAddress"
                           @bind-Location="_state.ToLocation"
                           Label="إلى" />

            <!-- Reassignment Section (if courier changed) -->
            @if (IsEdit && _state.DeliverymanId != _originalDeliverymanId)
            {
                <div class="rounded-xl border border-amber-200 bg-amber-50 p-4">
                    <div class="mb-2 text-sm text-amber-800">
                        سيُسجَّل تغيير المندوب كعملية إعادة إسناد، ولن تُسجَّل التغييرات الأخرى حقلاً بحقل.
                    </div>
                    <MudTextField @bind-Value="_state.ReassignReason"
                                  Label="سبب إعادة الإسناد"
                                  Required="true"
                                  MaxLength="200" />
                </div>
            }
        </div>
    </ChildContent>
</PageScaffold>
```

## 6. Integration Points

### 6.1 Numbering Integration

```csharp
// Uses existing INumberSequenceService
var allocation = await _numberSequenceService.NextMonthlyAsync(
    NumberSequenceKind.Deliveryman,  // Existing kind
    deliverymanId,
    createdAtOffset,  // Must be UTC DateTimeOffset
    cancellationToken);

// Allocation is inside an explicit transaction
await _unitOfWork.BeginTransactionAsync(cancellationToken);
try
{
    // ... allocate, assign, save ...
    await _unitOfWork.CommitTransactionAsync(cancellationToken);
}
catch
{
    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
    throw;
}
```

### 6.2 History Protection Integration

```csharp
// In AppDbContext.SaveChangesAsync
protected override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
{
    ProtectHistory();  // Existing method
    // ... rest of save logic
}

private void ProtectHistory()
{
    var modifiedOrDeletedHistory = ChangeTracker.Entries()
        .Any(e => (e.State == EntityState.Modified || e.State == EntityState.Deleted)
                  && (e.Entity is OrderStatusHistory
                      or DeliveryStepStatusHistory
                      or WorkRecordStatusHistory
                      or OrderDeliveryMovement
                      or StandaloneTripHistoryEntry));  // Add this type

    if (modifiedOrDeletedHistory)
    {
        throw new InvalidOperationException("لا يمكن تعديل أو حذف سجلات التاريخ.");
    }
}
```

### 6.3 Delete Guard Integration

```csharp
// New shared interface in OrderHub.Application/Interfaces
public interface ITripUsageReader
{
    Task<bool> HasDeliverymanTripsAsync(int deliverymanId, CancellationToken cancellationToken);
    Task<bool> HasPartyTripsAsync(TripPartyType partyType, int partyId, CancellationToken cancellationToken);
}

// In DeleteDeliverymanCommandHandler
public class DeleteDeliverymanCommandHandler : IRequestHandler<DeleteDeliverymanCommand, Result>
{
    private readonly IDeliverymanRepository _repository;
    private readonly ITripUsageReader _tripUsageReader;  // Add this

    public async Task<Result> Handle(
        DeleteDeliverymanCommand request,
        CancellationToken cancellationToken)
    {
        // ... existing checks ...

        // Add trips check BEFORE Remove()
        if (await _tripUsageReader.HasDeliverymanTripsAsync(deliveryman.Id, cancellationToken))
        {
            return Result.Failure(
                "لا يمكن حذف المندوب لارتباطه بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة.",
                "Conflict");
        }

        // ... rest of delete logic ...
    }
}
```

### 6.4 Navigation Integration

```csharp
// In NavMenu.razor.cs
private List<NavGroupModel> BuildGroups()
{
    return new List<NavGroupModel>
    {
        // ... existing groups ...

        new NavGroupModel
        {
            Title = "المشاوير",
            Subtitle = "مشاوير مستقلة",
            Icon = Icons.Material.Outlined.Route,
            ViewAllHref = "/trips",
            Items = new List<NavItemModel>
            {
                new NavItemModel
                {
                    Label = "كل المشاوير",
                    Href = "/trips",
                    Feature = PermissionFeature.Trips,
                    Level = PermissionLevel.Read,
                    Tone = "amber",
                    Description = "إدارة مشاوير المناديب المستقلة"
                }
            },
            Create = new NavItemModel
            {
                Label = "إنشاء مشوار",
                Href = "/trips/create",
                Feature = PermissionFeature.Trips,
                Level = PermissionLevel.Write,
                Icon = Icons.Material.Filled.Add
            }
        }
    };
}

// Fix permission cache in OnParametersSetAsync
protected override async Task OnParametersSetAsync()
{
    var combinations = Groups
        .SelectMany(group => group.Items.Append(group.Create).OfType<NavItemModel>())
        .Select(item => (item.Feature, item.Level))
        .Distinct();

    _allowedPermissions = new HashSet<(PermissionFeature, PermissionLevel)>();
    
    foreach (var (feature, level) in combinations)
    {
        if (await PermissionAccess.IsAllowedAsync(feature, level))
        {
            _allowedPermissions.Add((feature, level));
        }
    }
}
```

### 6.5 Permission Integration

```csharp
// Add to PermissionFeature enum (Domain)
public enum PermissionFeature
{
    // ... existing features ...
    
    [Description("المشاوير")]
    Trips
}

// Add to RoleFeatureCatalog (Web)
public static class RoleFeatureCatalog
{
    public static string Name(PermissionFeature feature) => feature switch
    {
        // ... existing mappings ...
        PermissionFeature.Trips => "المشاوير",
        _ => feature.ToString()
    };

    public static string Description(PermissionFeature feature) => feature switch
    {
        // ... existing mappings ...
        PermissionFeature.Trips => "إدارة مشاوير المناديب المستقلة",
        _ => ""
    };

    public static string Icon(PermissionFeature feature) => feature switch
    {
        // ... existing mappings ...
        PermissionFeature.Trips => Icons.Material.Outlined.Route,
        _ => Icons.Material.Filled.Security
    };

    public static string Tone(PermissionFeature feature) => feature switch
    {
        // ... existing mappings ...
        PermissionFeature.Trips => "bg-amber-50 text-amber-600",
        _ => "bg-slate-50 text-slate-600"
    };
}
```

## 7. Testing Strategy

### 7.1 Domain Tests

```csharp
public class StandaloneTripDomainTests
{
    [Fact]
    public void Create_WithValidInput_SetsStatusToPending()
    {
        // Arrange
        var operation = CreateOperation();
        var from = TripParty.FromClient(CreateClient());
        var to = TripParty.External("John Doe", "+1234567890", "123 Street", null);

        // Act
        var trip = StandaloneTrip.Create(
            deliverymanId: 1,
            title: "Test Trip",
            description: "Description",
            from: from,
            to: to,
            scheduledAtUtc: DateTimeOffset.UtcNow,
            operation: operation);

        // Assert
        Assert.Equal(WorkRecordStatus.Pending, trip.Status);
        Assert.Null(trip.StartedAtUtc);
        Assert.Equal(0, trip.Attempts.Count);
        Assert.Single(trip.History);
        Assert.Equal(1, trip.Version);
    }

    [Fact]
    public void Start_OnNewTrip_CreatesAndStartsFirstAttempt()
    {
        // Arrange
        var trip = CreateTrip();
        var operation = CreateOperation();

        // Act
        trip.Start(operation);

        // Assert
        Assert.NotNull(trip.StartedAtUtc);
        Assert.Single(trip.Attempts);
        Assert.Equal(1, trip.Attempts.First().AttemptNumber);
        Assert.NotNull(trip.Attempts.First().StartedAtUtc);
        Assert.Equal(2, trip.History.Count); // Created + AttemptStarted
        Assert.Equal(2, trip.Version);
    }

    [Fact]
    public void Update_AfterStart_ThrowsInvalidOperationException()
    {
        // Arrange
        var trip = CreateTrip();
        trip.Start(CreateOperation());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            trip.Update(
                "New Title",
                null,
                trip.From,
                trip.To,
                null,
                trip.DeliverymanId,
                null,
                null,
                null,
                CreateOperation()));

        Assert.Contains("لا يمكن تعديل المشوار بعد بدء المحاولة الأولى", exception.Message);
    }

    // ... more tests
}
```

### 7.2 Application Tests

```csharp
public class CreateTripHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly CreateTripCommandHandler _handler;

    public CreateTripHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Set up handler with all dependencies
        // ...
    }

    [Fact]
    public async Task Handle_WithValidInput_ReturnsSuccessWithReceipt()
    {
        // Arrange
        var deliveryman = await SeedDeliveryman();
        var client = await SeedClient();
        
        var command = new CreateTripCommand(
            DeliverymanId: deliveryman.Id,
            Title: "Test Trip",
            Description: "Test Description",
            From: new TripPartyInput(TripPartyType.Client, client.Id, null, null, null, null),
            To: new TripPartyInput(TripPartyType.External, null, "John Doe", "+1234567890", "123 Street", null),
            ScheduledAtUtc: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(WorkRecordStatus.Pending, result.Value.Status);
        Assert.True(result.Value.TripNumber > 0);
        Assert.Equal(1, result.Value.Version);
    }

    [Fact]
    public async Task Handle_WithInvalidDeliveryman_ReturnsNotFound()
    {
        // Arrange
        var command = new CreateTripCommand(
            DeliverymanId: 999,
            Title: "Test Trip",
            Description: null,
            From: new TripPartyInput(TripPartyType.External, null, "Jane Doe", "+1234567890", "456 Street", null),
            To: new TripPartyInput(TripPartyType.External, null, "John Doe", "+0987654321", "789 Street", null),
            ScheduledAtUtc: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
        Assert.Contains("المندوب غير موجود", result.ErrorMessage);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
```

### 7.3 UI Component Tests

Manual testing checklist for UI components:

1. **Trips List**
   - [ ] Server-side paging with 10/25/50 options
   - [ ] Case-insensitive search on all fields
   - [ ] Status filter (multi-select)
   - [ ] Deliveryman filter
   - [ ] Date range filters (scheduled, created)
   - [ ] Filter reset button
   - [ ] Row click opens details
   - [ ] Actions visible only with Write permission
   - [ ] Empty state, loading state, error state
   - [ ] RTL layout, Arabic labels

2. **Trip Details**
   - [ ] Header displays all trip info
   - [ ] Party cards with all snapshot data
   - [ ] Attempts table with correct chips
   - [ ] History timeline with paging
   - [ ] Actions available per state
   - [ ] Confirmation dialogs for all actions
   - [ ] Success/error feedback
   - [ ] Version conflict handling

3. **Trip Editor**
   - [ ] Create mode vs Edit mode
   - [ ] Deliveryman autocomplete
   - [ ] Party type selector
   - [ ] Known party pickers
   - [ ] External party validation
   - [ ] Reassignment reason field
   - [ ] Reassignment warning notice
   - [ ] Save disabled when invalid
   - [ ] Server error display
   - [ ] Navigation after save

4. **Deliveryman Details**
   - [ ] Page loads without Trips permission
   - [ ] Trips section visible with Trips.Read
   - [ ] Summary counters
   - [ ] Standalone trips table with paging
   - [ ] Order-bound trips table with paging
   - [ ] Order links visible with Orders.Read
   - [ ] Actions work from deliveryman page
   - [ ] Create button prefills courier

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Trip Number Uniqueness

*For any* two standalone trips assigned to the same deliveryman in the same month and year, their trip numbers SHALL be unique and strictly sequential.

**Validates: Requirements FR-1.2, NFR-1.1**

### Property 2: Lifecycle State Consistency

*For any* trip at any point in time, the combination of Status, StartedAtUtc, and Attempts SHALL produce exactly one valid TripDisplayState, and that state SHALL match the current attempt configuration.

**Validates: Requirements FR-3.1, FR-3.2, FR-3.3**

### Property 3: Edit Immutability After Start

*For any* trip where StartedAtUtc is not null OR Attempts count is greater than zero, any attempt to modify Title, Description, From, To, or ScheduledAtUtc SHALL be rejected.

**Validates: Requirements FR-1.5, FR-4.1**

### Property 4: Single Open Attempt Invariant

*For any* trip at any point in time, there SHALL be at most one attempt with Status = Pending.

**Validates: Requirements FR-3.6**

### Property 5: History Immutability

*For any* history entry once created, its fields SHALL never be modified or deleted.

**Validates: Requirements FR-5.1, FR-5.3**

### Property 6: Snapshot Preservation After Start

*For any* trip where StartedAtUtc is not null, the From and To party snapshots SHALL remain unchanged for all subsequent operations.

**Validates: Requirements FR-2.4**

### Property 7: Reassignment Renumbering

*For any* trip where the DeliverymanId changes while IsNew is true, a new trip number SHALL be allocated from the new deliveryman's sequence in the original SequenceYear and SequenceMonth, and both old and new courier information SHALL be recorded in history.

**Validates: Requirements FR-4.3, FR-4.4**

### Property 8: Version Increment Atomicity

*For any* mutation operation on a trip, the Version field SHALL increment by exactly one, and this increment SHALL be atomic with the mutation.

**Validates: Requirements NFR-13.4, NFR-13.5**

### Property 9: Failed Attempt Persistence

*For any* trip that has at least one attempt with Status = Failed, that trip SHALL appear in failed-status filters and courier failure counters regardless of the trip's current header Status.

**Validates: Requirements FR-3.4, FR-7.5**

### Property 10: Terminal State Finality

*For any* trip with Status = Completed OR Status = Cancelled, all lifecycle operations (Start, Complete, Fail, Cancel, Retry) SHALL be rejected.

**Validates: Requirements FR-3.7**

### Property 11: Party Snapshot Completeness

*For any* known party type (Client, Supplier, ShippingCarrier), the snapshot SHALL include Name, and MAY include Phone and Address when present, with Address including the city exactly once, and Phone stored up to 22 characters without truncation.

**Validates: Requirements FR-2.2, NFR-1.3**

### Property 12: External Party Validation

*For any* External party, Name (1-100 chars), Phone (6-22 chars with ≥6 digits), and Address (1-255 chars) SHALL be required, and Location SHALL be optional (≤255 chars).

**Validates: Requirements FR-2.3**

### Property 13: Numbering Transaction Rollback

*For any* failed trip creation, if the database operation fails after number allocation, both the trip insertion and the sequence increment SHALL be rolled back atomically.

**Validates: Requirements NFR-1.1, NFR-1.2**

### Property 14: Master Data Protection

*For any* deliveryman, client, supplier, or shipping carrier that is referenced by at least one standalone trip (including historical references in trip reassignment history), deletion SHALL be blocked with a clear error message.

**Validates: Requirements FR-12.1, FR-12.2**

### Property 15: Permission-Based Visibility

*For any* user without Trips.Read permission, all trip data, navigation items, and UI sections SHALL be hidden, and for any user with Trips.Read but without Trips.Write, all mutating actions SHALL be hidden.

**Validates: Requirements FR-6.1, FR-6.3**

---

## Document Metadata

- **Prepared by**: AI Agent
- **Review Status**: Pending
- **Next Steps**: 
  1. Review and approve design document
  2. Create implementation task breakdown
  3. Begin Phase 1: Domain layer implementation
- **Dependencies**: Requirements document approval
- **Related Artifacts**: 
  - `requirements.md`
  - `TRIPS-MANAGEMENT-SPEC.md`
  - `TRIPS-MANAGEMENT-TASKS.md`
