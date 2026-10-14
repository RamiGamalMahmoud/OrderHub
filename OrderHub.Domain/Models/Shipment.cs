using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed class Shipment : ModelBase, IWorkRecord
{
    private readonly List<WorkRecordStatusHistory> _statusHistory = [];
    private int _persistedHistoryCount;
    private bool _initialHistoryCreated;
    private Shipment() { }
    internal Shipment(Order order, OrderDeliveryStep step)
    {
        if ((order is null) == (step is null)) throw new ArgumentException("يجب ربط الشحنة بطلب أو بخطوة توصيل واحدة.");
        Order = order;
        OrderId = order?.Id;
        DeliveryStep = step;
        DeliveryStepId = step?.Id;
        Status = WorkRecordStatus.Pending;
    }
    public int? OrderId { get; private set; }
    public Order Order { get; private set; }
    public int? DeliveryStepId { get; private set; }
    public OrderDeliveryStep DeliveryStep { get; private set; }
    public WorkRecordStatus Status { get; private set; }
    public int NumberingPartyId { get; private set; }
    public long WorkRecordNumber { get; private set; }
    public int SequenceYear { get; private set; }
    public int SequenceMonth { get; private set; }
    NumberSequenceKind IWorkRecord.NumberSequenceKind => NumberSequenceKind.ShippingCarrier;
    int IWorkRecord.NumberingPartyId => NumberingPartyId > 0
        ? NumberingPartyId
        : DeliveryStep?.ShippingCarrierId ?? Order?.ShippingCarrierId
            ?? throw new InvalidOperationException("لا يمكن ترقيم شحنة دون شركة شحن محددة.");
    long IWorkRecord.WorkRecordNumber => WorkRecordNumber;
    bool IWorkRecord.HasWorkRecordNumber => WorkRecordNumber > 0;
    void IWorkRecord.AssignWorkRecordNumber(int partyId, long number, int year, int month)
    {
        if (WorkRecordNumber > 0) throw new InvalidOperationException("رقم الشحنة لا يمكن تغييره بعد تخصيصه.");
        if (partyId != ((IWorkRecord)this).NumberingPartyId) throw new InvalidOperationException("لا يمكن نقل هوية الشحنة إلى شركة شحن أخرى.");
        if (number <= 0 || year <= 0 || month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(number));
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
    IEnumerable<WorkRecordStatusHistory> IWorkRecord.UnpersistedStatusHistory => _statusHistory.Skip(_persistedHistoryCount);
    void IWorkRecord.AppendInitialStatusHistory() { if (!_initialHistoryCreated) { _statusHistory.Insert(0, new(WorkRecordType.Shipment, Id, WorkRecordStatus.Pending, null, CreatedAt)); _initialHistoryCreated = true; } }
    void IWorkRecord.MarkStatusHistoryPersisted() => _persistedHistoryCount = _statusHistory.Count;
    public void TransitionTo(WorkRecordStatus target, string reason = null, DateTime? atUtc = null)
    {
        if (Status == target) return;
        var at = atUtc ?? DateTime.UtcNow;
        WorkRecordLifecycle.Transition(this, WorkRecordType.Shipment, Id, Status, target, reason, _statusHistory, at);
        Status = target;
        UpdateModificationDate(at);
    }
}
