using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed class Supply : ModelBase, IWorkRecord
{
    private readonly List<WorkRecordStatusHistory> _statusHistory = [];
    private int _persistedHistoryCount;
    private bool _initialHistoryCreated;
    private Supply() { }

    public Supply(Order order, int supplierId)
    {
        Order = order ?? throw new ArgumentNullException(nameof(order));
        OrderId = order.Id;
        if (supplierId <= 0) throw new ArgumentOutOfRangeException(nameof(supplierId));
        SupplierId = supplierId;
        Status = WorkRecordStatus.Pending;
    }

    public int OrderId { get; private set; }
    public Order Order { get; private set; }
    public int SupplierId { get; private set; }
    public Supplier Supplier { get; private set; }
    public WorkRecordStatus Status { get; private set; }
    public long WorkRecordNumber { get; private set; }
    public int SequenceYear { get; private set; }
    public int SequenceMonth { get; private set; }
    NumberSequenceKind IWorkRecord.NumberSequenceKind => NumberSequenceKind.Supplier;
    int IWorkRecord.NumberingPartyId => SupplierId;
    long IWorkRecord.WorkRecordNumber => WorkRecordNumber;
    bool IWorkRecord.HasWorkRecordNumber => WorkRecordNumber > 0;
    void IWorkRecord.AssignWorkRecordNumber(int partyId, long number, int year, int month)
    {
        if (WorkRecordNumber > 0) throw new InvalidOperationException("رقم التوريد لا يمكن تغييره بعد تخصيصه.");
        if (partyId != SupplierId) throw new InvalidOperationException("لا يمكن نقل هوية التوريد إلى مورد آخر.");
        if (number <= 0 || year <= 0 || month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(number));
        WorkRecordNumber = number;
        SequenceYear = year;
        SequenceMonth = month;
    }
    void IWorkRecord.ClearWorkRecordNumber()
    {
        WorkRecordNumber = 0;
        SequenceYear = 0;
        SequenceMonth = 0;
    }
    public IReadOnlyCollection<WorkRecordStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    IEnumerable<WorkRecordStatusHistory> IWorkRecord.UnpersistedStatusHistory => _statusHistory.Skip(_persistedHistoryCount);
    void IWorkRecord.AppendInitialStatusHistory() { if (!_initialHistoryCreated) { _statusHistory.Insert(0, new(WorkRecordType.SupplierSupply, Id, WorkRecordStatus.Pending, null, CreatedAt)); _initialHistoryCreated = true; } }
    void IWorkRecord.MarkStatusHistoryPersisted() => _persistedHistoryCount = _statusHistory.Count;

    public void TransitionTo(WorkRecordStatus target, string reason = null, DateTime? atUtc = null)
    {
        if (Status == target) return;
        WorkRecordLifecycle.Transition(this, WorkRecordType.SupplierSupply, Id, Status, target, reason, _statusHistory, atUtc ?? DateTime.UtcNow);
        Status = target;
        UpdateModificationDate(atUtc ?? DateTime.UtcNow);
    }
}
