using System;
using System.Collections.Generic;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

internal static class WorkRecordLifecycle
{
    internal static void Transition(ModelBase record, WorkRecordType type, int id,
        WorkRecordStatus current, WorkRecordStatus target, string reason,
        ICollection<WorkRecordStatusHistory> history, DateTime atUtc)
    {
        if (current == target) return;
        if (current != WorkRecordStatus.Pending)
            throw new InvalidOperationException("المحاولة غير معلقة ولا يمكن تغيير حالتها. أنشئ محاولة جديدة لإعادة المحاولة.");
        if (target is WorkRecordStatus.Pending || !Enum.IsDefined(target))
            throw new InvalidOperationException("انتقال الحالة المطلوب غير صالح.");
        if (target is WorkRecordStatus.Failed or WorkRecordStatus.Cancelled && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("السبب مطلوب للحالات الفاشلة والملغاة.");
        if (reason?.Trim().Length > 200) throw new InvalidOperationException("السبب لا يتجاوز 200 حرف.");
        if (atUtc.Kind != DateTimeKind.Utc) throw new InvalidOperationException("يجب أن يكون الوقت بتوقيت UTC.");
        history.Add(new WorkRecordStatusHistory(type, id, target, reason, atUtc));
    }
}
