using System;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed class WorkRecordStatusHistory
{
    private WorkRecordStatusHistory() { }

    internal WorkRecordStatusHistory(WorkRecordType workRecordType, int workRecordId,
        WorkRecordStatus status, string reason, DateTime createdAtUtc)
    {
        if (!Enum.IsDefined(workRecordType) || workRecordId < 0 || !Enum.IsDefined(status))
            throw new ArgumentException("بيانات سجل حالة التشغيل غير صالحة.");
        if (status is WorkRecordStatus.Failed or WorkRecordStatus.Cancelled && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("السبب مطلوب للحالات الفاشلة والملغاة.", nameof(reason));
        if (reason?.Trim().Length > 200) throw new ArgumentException("السبب لا يتجاوز 200 حرف.", nameof(reason));
        if (createdAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("يجب أن يكون الوقت بتوقيت UTC.", nameof(createdAtUtc));
        WorkRecordType = workRecordType;
        WorkRecordId = workRecordId;
        Status = status;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public int Id { get; private set; }
    public WorkRecordType WorkRecordType { get; private set; }
    public int WorkRecordId { get; private set; }
    public WorkRecordStatus Status { get; private set; }
    public string Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    internal void SetWorkRecordId(int id)
    {
        if (WorkRecordId != 0 || id <= 0) throw new InvalidOperationException("معرف سجل التشغيل غير صالح.");
        WorkRecordId = id;
    }
}
