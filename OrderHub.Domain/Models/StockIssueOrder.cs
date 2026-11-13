using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

/// <summary>
/// أمر صرف يدوي لكميات محجوزة من مخزن واحد، يُرقَّم شهريًا لكل مخزن ويُنفَّذ بدفعات.
/// </summary>
public sealed class StockIssueOrder : ModelBase, IWorkRecord
{
    public const int ReasonMaxLength = 200;

    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    private readonly List<StockIssueLine> _lines = [];
    private readonly List<WorkRecordStatusHistory> _statusHistory = [];
    private int _persistedHistoryCount;
    private bool _initialHistoryCreated;

    private StockIssueOrder() { }

    private StockIssueOrder(Order order, Warehouse warehouse, int requestedByUserId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestedByUserId);

        Order = order;
        OrderId = order.Id;
        Warehouse = warehouse;
        WarehouseId = warehouse.Id;
        RequestedByUserId = requestedByUserId;
        AssignedUserId = warehouse.KeeperUserId;
        Status = WorkRecordStatus.Pending;
        UpdateCreationDate(nowUtc);
    }

    public static Result<StockIssueOrder> Create(
        Order order,
        Warehouse warehouse,
        int requestedByUserId,
        IReadOnlyCollection<StockIssueLineData> lines,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (order.Id <= 0)
            return Result<StockIssueOrder>.Failure("يجب حفظ الطلب قبل إنشاء أمر صرف.");
        if (warehouse.Id <= 0)
            return Result<StockIssueOrder>.Failure("المخزن غير صالح.");
        if (lines.Count == 0)
            return Result<StockIssueOrder>.Failure("أضف بندًا واحدًا على الأقل لأمر الصرف.");

        var issueOrder = new StockIssueOrder(order, warehouse, requestedByUserId, nowUtc);
        foreach (StockIssueLineData line in lines)
        {
            Result added = issueOrder.AddLine(line.AllocationId, line.ProductId, line.RequestedQty);
            if (!added.IsSuccess)
                return Result<StockIssueOrder>.Failure(added.ErrorMessage);
        }

        return Result<StockIssueOrder>.Success(issueOrder);
    }

    public int OrderId { get; private set; }

    public Order Order { get; private set; }

    public int WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    public int RequestedByUserId { get; private set; }

    public User RequestedBy { get; private set; }

    /// <summary>لقطة عن أمين المخزن وقت الإنشاء لأغراض التدقيق فقط؛ التنفيذ يتبع الربط الحالي.</summary>
    public int AssignedUserId { get; private set; }

    public User AssignedTo { get; private set; }

    public WorkRecordStatus Status { get; private set; }

    public int? CancelRequestedByUserId { get; private set; }

    public User CancelRequestedBy { get; private set; }

    public string CancelReason { get; private set; }

    public DateTime? CancelRequestedAtUtc { get; private set; }

    public int Version { get; private set; }

    public int NumberingPartyId { get; private set; }

    public long WorkRecordNumber { get; private set; }

    public int SequenceYear { get; private set; }

    public int SequenceMonth { get; private set; }

    public IReadOnlyCollection<StockIssueLine> Lines => _lines.AsReadOnly();

    public bool IsTerminal => Status is WorkRecordStatus.Completed or WorkRecordStatus.Cancelled;

    public bool AcceptsBatches => Status is WorkRecordStatus.Pending or WorkRecordStatus.PartiallyExecuted;

    public int RequestedTotal => _lines.Sum(line => line.RequestedQty);

    public int IssuedTotal => _lines.Sum(line => line.IssuedQty);

    NumberSequenceKind IWorkRecord.NumberSequenceKind => NumberSequenceKind.Warehouse;

    int IWorkRecord.NumberingPartyId => NumberingPartyId > 0 ? NumberingPartyId : WarehouseId;

    long IWorkRecord.WorkRecordNumber => WorkRecordNumber;

    bool IWorkRecord.HasWorkRecordNumber => WorkRecordNumber > 0;

    void IWorkRecord.AssignWorkRecordNumber(int partyId, long number, int year, int month)
    {
        if (WorkRecordNumber > 0)
            throw new InvalidOperationException("رقم أمر الصرف لا يمكن تغييره بعد تخصيصه.");
        if (partyId != WarehouseId)
            throw new InvalidOperationException("لا يمكن نقل ترقيم أمر الصرف إلى مخزن آخر.");
        if (number <= 0 || year <= 0 || month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(number));

        NumberingPartyId = partyId;
        WorkRecordNumber = number;
        SequenceYear = year;
        SequenceMonth = month;
    }

    void IWorkRecord.ClearWorkRecordNumber()
    {
        NumberingPartyId = 0;
        WorkRecordNumber = 0;
        SequenceYear = 0;
        SequenceMonth = 0;
    }

    public IReadOnlyCollection<WorkRecordStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    IEnumerable<WorkRecordStatusHistory> IWorkRecord.UnpersistedStatusHistory =>
        _statusHistory.Skip(_persistedHistoryCount);

    void IWorkRecord.AppendInitialStatusHistory()
    {
        if (_initialHistoryCreated)
            return;

        _statusHistory.Insert(0, new WorkRecordStatusHistory(
            WorkRecordType.StockIssueOrder, Id, WorkRecordStatus.Pending, null, CreatedAt));
        _initialHistoryCreated = true;
    }

    void IWorkRecord.MarkStatusHistoryPersisted() => _persistedHistoryCount = _statusHistory.Count;

    internal Result AddLine(int allocationId, int productId, int requestedQty)
    {
        if (allocationId <= 0 || productId <= 0)
            return Result.Failure("بند أمر الصرف غير صالح.");
        if (requestedQty <= 0)
            return Result.Failure("كمية الصرف يجب أن تكون أكبر من صفر.");
        if (_lines.Any(line => line.AllocationId == allocationId))
            return Result.Failure("لا يمكن تكرار التخصيص نفسه في أمر الصرف.");

        _lines.Add(new StockIssueLine(allocationId, productId, requestedQty));
        return Result.Success();
    }

    public Result RecordBatches(
        IReadOnlyCollection<StockIssueBatchData> batches,
        int actorUserId,
        DateTime effectiveAtUtc,
        bool final,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(batches);

        if (!AcceptsBatches)
            return Result.Failure(Status == WorkRecordStatus.CancelRequested
                ? "طُلب إلغاء أمر الصرف ولا يمكن تسجيل دفعات جديدة."
                : "أمر الصرف مغلق ولا يمكن تسجيل دفعات جديدة.");

        if (batches.Count == 0)
            return Result.Failure("أضف كمية واحدة على الأقل للدفعة.");

        var effective = DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc);
        if (effective == default)
            return Result.Failure("وقت الصرف مطلوب.");
        if (effective > DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc).Add(FutureTolerance))
            return Result.Failure("لا يمكن أن يكون وقت الصرف في المستقبل.");

        var validated = new List<(StockIssueLine Line, int Quantity, string ShortfallReason)>();
        var seenLines = new HashSet<int>();
        foreach (StockIssueBatchData batch in batches)
        {
            StockIssueLine line = _lines.FirstOrDefault(candidate => candidate.Id == batch.LineId);
            if (line is null)
                return Result.Failure("أحد بنود أمر الصرف غير موجود.");

            if (!seenLines.Add(batch.LineId))
                return Result.Failure("لا يمكن تكرار البند نفسه في الدفعة.");

            if (batch.Quantity <= 0)
                return Result.Failure("كمية الدفعة يجب أن تكون أكبر من صفر.");

            if (line.IssuedQty + batch.Quantity > line.RequestedQty)
                return Result.Failure("كمية الدفعة تتجاوز الكمية المتبقية من البند.");

            string reason = NormalizeReason(batch.ShortfallReason, StockIssueBatch.ReasonMaxLength);
            if (final && line.IssuedQty + batch.Quantity < line.RequestedQty && reason is null)
                return Result.Failure("أدخل سبب النقص لكل بند لم تُصرف كميته كاملة قبل إغلاق الصرف.");

            validated.Add((line, batch.Quantity, reason));
        }

        foreach ((StockIssueLine line, int quantity, string reason) in validated)
            line.AddBatch(quantity, actorUserId, effective, reason, nowUtc);

        Touch();

        WorkRecordStatus target = final || _lines.All(line => line.IssuedQty >= line.RequestedQty)
            ? WorkRecordStatus.Completed
            : _lines.Any(line => line.IssuedQty > 0)
                ? WorkRecordStatus.PartiallyExecuted
                : WorkRecordStatus.Pending;

        MoveTo(target, null, nowUtc);
        return Result.Success();
    }

    public Result RequestCancellation(int keeperUserId, string reason, DateTime nowUtc)
    {
        if (Status is not (WorkRecordStatus.Pending or WorkRecordStatus.PartiallyExecuted))
        {
            return Result.Failure(Status == WorkRecordStatus.CancelRequested
                ? "سبق إرسال طلب إلغاء هذا الأمر."
                : "أمر الصرف مغلق ولا يمكن طلب إلغائه.");
        }

        string normalized = NormalizeReason(reason);
        if (normalized is null)
            return Result.Failure("سبب طلب الإلغاء مطلوب.");

        CancelRequestedByUserId = keeperUserId;
        CancelReason = normalized;
        CancelRequestedAtUtc = nowUtc;
        MoveTo(WorkRecordStatus.CancelRequested, normalized, nowUtc);
        return Result.Success();
    }

    /// <summary>System cancellation on order cancel/archive; also closes a pending cancel request.</summary>
    public Result Cancel(string reason, DateTime nowUtc)
    {
        if (IsTerminal)
            return Result.Success();

        string normalized = NormalizeReason(reason);
        if (normalized is null)
            return Result.Failure("سبب الإلغاء مطلوب.");

        MoveTo(WorkRecordStatus.Cancelled, normalized, nowUtc);
        return Result.Success();
    }

    private void MoveTo(WorkRecordStatus target, string reason, DateTime atUtc)
    {
        if (Status == target)
            return;

        if (!Enum.IsDefined(target))
            throw new InvalidOperationException("انتقال حالة أمر الصرف غير صالح.");
        if (target is WorkRecordStatus.Cancelled or WorkRecordStatus.CancelRequested &&
            string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("السبب مطلوب لطلب الإلغاء والإلغاء.");

        _statusHistory.Add(new WorkRecordStatusHistory(
            WorkRecordType.StockIssueOrder, Id, target, reason, atUtc));
        Status = target;
        UpdateModificationDate(atUtc);
    }

    private void Touch()
    {
        Version++;
        UpdateModificationDate(DateTime.UtcNow);
    }

    private static string NormalizeReason(string reason, int maxLength = ReasonMaxLength)
    {
        string trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        if (trimmed.Length > maxLength)
            throw new ArgumentOutOfRangeException(nameof(reason), $"السبب لا يتجاوز {maxLength} حرف.");
        return trimmed;
    }
}

public readonly record struct StockIssueLineData(int AllocationId, int ProductId, int RequestedQty);

public readonly record struct StockIssueBatchData(int LineId, int Quantity, string ShortfallReason);
