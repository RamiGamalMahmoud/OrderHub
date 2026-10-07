using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public partial class Order
{
    private readonly List<OrderStatusHistory> _statusHistory = [];
    private readonly List<DeliveryStepStatusHistory> _stepHistory = [];

    public int Version { get; private set; } = 1;
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAtUtc { get; private set; }
    public int? ArchivedByUserId { get; private set; }
    public string ArchivedByUserName { get; private set; }
    public string ArchiveReason { get; private set; }
    public bool CanEdit => !IsArchived && OrderStatus == OrderStatus.Processing;
    public bool IsFinal => OrderStatus is OrderStatus.Delivered or OrderStatus.Cancelled;
    public bool CanArchive => !IsArchived && (CanEdit || IsFinal);
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<DeliveryStepStatusHistory> StepHistory => _stepHistory.AsReadOnly();

    public Result CheckVersion(int expected) => expected == Version
        ? Result.Success() : Result.Failure("تم تعديل الطلب بواسطة مستخدم آخر. حدّث البيانات ثم راجع العملية.");

    public Result InitializeHistory(OrderOperation operation)
    {
        var valid = ValidateOperation(operation);
        if (!valid.IsSuccess) return valid;
        if (_statusHistory.Count != 0 || OrderStatus != OrderStatus.Processing || operation.AtUtc != CreatedAt)
            return Result.Failure("سجل بداية الطلب غير صالح.");
        _statusHistory.Add(new(null, OrderStatus.Processing, operation, null, OrderHistorySource.Creation, Version));
        return Result.Success();
    }

    public string DeliveryReadinessError()
    {
        if (!DeliveryMethod.HasValue || !Enum.IsDefined(DeliveryMethod.Value)) return "يجب تحديد طريقة التوصيل.";
        if (DeliveryMethod == Enums.DeliveryMethod.DeliveryMan && DeliverymanId is not > 0) return "يجب تحديد المندوب.";
        if (DeliveryMethod == Enums.DeliveryMethod.ShippingCompany && ShippingCarrierId is not > 0) return "يجب تحديد شركة الشحن.";
        if (DeliveryMethod == Enums.DeliveryMethod.DeliveryChain)
        {
            if (_deliverySteps.Count == 0) return "يجب إضافة خطوة توصيل واحدة على الأقل.";
            if (_deliverySteps.Select(step => step.StepOrder).Distinct().Count() != _deliverySteps.Count)
                return "ترتيب خطوات التوصيل مكرر.";
            foreach (var step in _deliverySteps.OrderBy(step => step.StepOrder))
                if (step.StepOrder <= 0 || !ValidStepHandler(step))
                    return $"جهة تنفيذ الخطوة {step.StepOrder} غير صالحة.";
        }
        return null;
    }

    public IReadOnlyList<OrderStatus> AllowedStatuses()
    {
        if (IsArchived || IsFinal) return [];
        var statuses = new List<OrderStatus>();
        switch (OrderStatus)
        {
            case OrderStatus.Processing when DeliveryReadinessError() is null:
                statuses.Add(DeliveryMethod switch
                {
                    Enums.DeliveryMethod.Pickup => OrderStatus.ReadyForPickup,
                    Enums.DeliveryMethod.ShippingCompany => OrderStatus.Shipped,
                    _ => OrderStatus.OutForDelivery
                });
                break;
            case OrderStatus.ReadyForPickup when DeliveryMethod == Enums.DeliveryMethod.Pickup:
                statuses.Add(OrderStatus.Delivered);
                break;
            case OrderStatus.OutForDelivery when DeliveryMethod == Enums.DeliveryMethod.DeliveryMan:
            case OrderStatus.Shipped when DeliveryMethod == Enums.DeliveryMethod.ShippingCompany:
                statuses.AddRange([OrderStatus.Delivered, OrderStatus.DeliveryFailed]);
                break;
            case OrderStatus.DeliveryFailed when DeliveryMethod == Enums.DeliveryMethod.DeliveryMan:
                statuses.Add(OrderStatus.OutForDelivery);
                break;
            case OrderStatus.DeliveryFailed when DeliveryMethod == Enums.DeliveryMethod.ShippingCompany:
                statuses.Add(OrderStatus.Shipped);
                break;
        }
        statuses.Add(OrderStatus.Cancelled);
        return statuses;
    }

    public Result TransitionTo(OrderStatus target, OrderOperation operation, string reason = null)
    {
        var valid = ValidateOperation(operation);
        if (!valid.IsSuccess) return valid;
        if (IsArchived) return Result.Failure("الطلب مؤرشف ولا يقبل التشغيل.");
        if (target == OrderStatus) return Result.Success();
        if (!AllowedStatuses().Contains(target)) return Result.Failure("الانتقال المطلوب غير مسموح لطريقة التوصيل والحالة الحالية.");
        valid = ValidateReason(reason, target is OrderStatus.Cancelled or OrderStatus.DeliveryFailed);
        if (!valid.IsSuccess) return valid;

        var source = target == OrderStatus.Cancelled ? OrderHistorySource.OrderCancellation
            : OrderStatus == OrderStatus.DeliveryFailed ? OrderHistorySource.Retry : OrderHistorySource.ManualAction;
        Version++;
        if (target == OrderStatus.Cancelled)
            foreach (var step in _deliverySteps.Where(step => step.Status is not (DeliveryStatus.Completed or DeliveryStatus.Cancelled)))
                RecordStep(step, DeliveryStatus.Cancelled, operation, reason, source);
        RecordOrder(target, operation, reason, source);
        return Result.Success();
    }

    public IReadOnlyList<DeliveryStatus> AllowedStepStatuses(OrderDeliveryStep step)
    {
        if (IsArchived || OrderStatus != OrderStatus.OutForDelivery || DeliveryMethod != Enums.DeliveryMethod.DeliveryChain ||
            !_deliverySteps.Any(existing => ReferenceEquals(existing, step)) || !ValidStepHandler(step) ||
            _deliverySteps.Any(previous => previous.StepOrder < step.StepOrder && previous.Status != DeliveryStatus.Completed)) return [];
        return step.Status switch
        {
            DeliveryStatus.Pending or DeliveryStatus.Failed => [DeliveryStatus.InProgress],
            DeliveryStatus.InProgress => [DeliveryStatus.Completed, DeliveryStatus.Failed],
            _ => []
        };
    }

    public Result TransitionStep(int stepId, DeliveryStatus target, OrderOperation operation, string reason = null)
    {
        var valid = ValidateOperation(operation);
        if (!valid.IsSuccess) return valid;
        var step = _deliverySteps.SingleOrDefault(step => step.Id == stepId);
        if (step is null) return Result.Failure("خطوة التوصيل غير موجودة في الطلب.");
        if (IsArchived || IsFinal) return Result.Failure("الطلب مغلق ولا يقبل تشغيل الخطوات.");
        if (step.Status == target) return Result.Success();
        if (!AllowedStepStatuses(step).Contains(target)) return Result.Failure("لا يمكن تنفيذ الخطوة قبل اكتمال السابقة أو من حالتها الحالية.");
        valid = ValidateReason(reason, target == DeliveryStatus.Failed);
        if (!valid.IsSuccess) return valid;
        var source = step.Status == DeliveryStatus.Failed ? OrderHistorySource.Retry : OrderHistorySource.ManualAction;
        Version++;
        RecordStep(step, target, operation, reason, source);
        if (_deliverySteps.Count > 0 && _deliverySteps.All(step => step.Status == DeliveryStatus.Completed))
            RecordOrder(OrderStatus.Delivered, operation, null, OrderHistorySource.LastStepCompleted);
        return Result.Success();
    }

    public Result DeliverDirectly(IReadOnlyCollection<DeliveryStepOutcome> outcomes, OrderOperation operation, string reason)
    {
        var valid = ValidateOperation(operation);
        if (!valid.IsSuccess) return valid;
        valid = ValidateReason(reason, true);
        if (!valid.IsSuccess) return valid;
        if (IsArchived || OrderStatus != OrderStatus.OutForDelivery || DeliveryMethod != Enums.DeliveryMethod.DeliveryChain || DeliveryReadinessError() is not null)
            return Result.Failure("التسليم المباشر غير متاح لهذا الطلب.");
        if (outcomes is null || outcomes.Any(item => item is null) || outcomes.Select(item => item.StepId).Distinct().Count() != outcomes.Count)
            return Result.Failure("نتائج الخطوات غير صالحة أو مكررة.");
        var remaining = _deliverySteps.Where(step => step.Status != DeliveryStatus.Completed).ToList();
        if (outcomes.Count != remaining.Count || outcomes.Any(item => !remaining.Any(step => step.Id == item.StepId)))
            return Result.Failure("يجب مراجعة جميع الخطوات غير المكتملة دون تغيير الخطوات المكتملة.");
        foreach (var outcome in outcomes)
        {
            var step = remaining.Single(step => step.Id == outcome.StepId);
            if (outcome.Status != step.Status && outcome.Status is not (DeliveryStatus.Completed or DeliveryStatus.Failed or DeliveryStatus.Cancelled))
                return Result.Failure("نتيجة الخطوة غير مسموحة في التسليم المباشر.");
            valid = ValidateReason(outcome.Reason, outcome.Status != step.Status && outcome.Status is DeliveryStatus.Failed or DeliveryStatus.Cancelled);
            if (!valid.IsSuccess) return valid;
        }
        Version++;
        foreach (var outcome in outcomes)
            RecordStep(remaining.Single(step => step.Id == outcome.StepId), outcome.Status, operation, outcome.Reason, OrderHistorySource.DirectDelivery);
        RecordOrder(OrderStatus.Delivered, operation, reason, OrderHistorySource.DirectDelivery);
        return Result.Success();
    }

    public Result Archive(OrderOperation operation, string reason)
    {
        var valid = ValidateOperation(operation);
        if (!valid.IsSuccess) return valid;
        valid = ValidateReason(reason, true);
        if (!valid.IsSuccess) return valid;
        if (!CanArchive) return Result.Failure("يجب إلغاء الطلب النشط قبل أرشفته، ولا يمكن استعادة المؤرشف.");
        IsArchived = true;
        ArchivedAtUtc = operation.AtUtc;
        ArchivedByUserId = operation.UserId;
        ArchivedByUserName = operation.UserName.Trim();
        ArchiveReason = reason.Trim();
        Version++;
        return Result.Success();
    }

    public Result TouchForEdit()
    {
        if (!CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        Version++;
        return Result.Success();
    }

    private void RecordOrder(OrderStatus target, OrderOperation operation, string reason, OrderHistorySource source)
    {
        _statusHistory.Add(new(OrderStatus, target, operation, reason, source, Version));
        OrderStatus = target;
        UpdateModificationDate(operation.AtUtc);
    }

    private void RecordStep(OrderDeliveryStep step, DeliveryStatus target, OrderOperation operation, string reason, OrderHistorySource source)
    {
        if (step.Status == target) return;
        _stepHistory.Add(new(step, step.Status, target, operation, reason, source, Version));
        step.RecordStatus(target);
        step.UpdateModificationDate(operation.AtUtc);
    }

    private static bool ValidStepHandler(OrderDeliveryStep step) => step.DeliveryMethod switch
    {
        Enums.DeliveryMethod.DeliveryMan => step.DeliverymanId is > 0 && step.ShippingCarrierId is null,
        Enums.DeliveryMethod.ShippingCompany => step.ShippingCarrierId is > 0 && step.DeliverymanId is null,
        _ => false
    };

    private static Result ValidateOperation(OrderOperation operation) => operation is not null && operation.UserId > 0 &&
        !string.IsNullOrWhiteSpace(operation.UserName) && operation.UserName.Trim().Length <= 150 && operation.AtUtc.Kind == DateTimeKind.Utc
        ? Result.Success() : Result.Failure("المستخدم أو وقت العملية غير صالح.");

    public static Result ValidateReason(string reason, bool required) =>
        required && string.IsNullOrWhiteSpace(reason) || reason?.Trim().Length > 200
            ? Result.Failure("السبب مطلوب ويجب ألا يتجاوز 200 حرف.") : Result.Success();
}
