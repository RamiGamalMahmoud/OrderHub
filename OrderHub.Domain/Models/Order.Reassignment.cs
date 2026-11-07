using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed record DeliveryParty(int PartyId, string Name);

public sealed record DeliveryStepReassignment(OrderDeliveryStep CancelledStep, OrderDeliveryStep ReplacementStep);

public partial class Order
{
    private readonly List<OrderDeliveryMovement> _deliveryMovements = [];

    public IReadOnlyCollection<OrderDeliveryMovement> DeliveryMovements => _deliveryMovements.AsReadOnly();

    public Result<DeliveryStepReassignment> ReassignDeliveryStep(int stepId, DeliveryMethod method,
        DeliveryParty target, string previousPartyName, OrderOperation operation, string reason)
    {
        var valid = ValidateOperation(operation);
        if (!valid.IsSuccess) return Result<DeliveryStepReassignment>.Failure(valid.ErrorMessage);
        valid = ValidateReason(reason, true);
        if (!valid.IsSuccess) return Result<DeliveryStepReassignment>.Failure(valid.ErrorMessage);
        if (IsArchived) return Result<DeliveryStepReassignment>.Failure("الطلب مؤرشف ولا يقبل التشغيل.");
        if (IsFinal) return Result<DeliveryStepReassignment>.Failure("الطلب مغلق ولا يقبل إعادة التوجيه.");
        if (DeliveryMethod != Enums.DeliveryMethod.DeliveryChain)
            return Result<DeliveryStepReassignment>.Failure("إعادة توجيه الخطوات متاحة لسلسلة التوصيل فقط.");

        var step = _deliverySteps.SingleOrDefault(item => item.Id == stepId);
        if (step is null) return Result<DeliveryStepReassignment>.Failure("خطوة التوصيل غير موجودة في الطلب.");
        if (step.StepOrder <= 0) return Result<DeliveryStepReassignment>.Failure("الخطوة ملغاة أو مستبدلة.");
        if (step.Status is not (DeliveryStatus.Pending or DeliveryStatus.InProgress or DeliveryStatus.Failed))
            return Result<DeliveryStepReassignment>.Failure("لا يمكن إعادة توجيه الخطوة من حالتها الحالية.");

        var previousMethod = step.DeliveryMethod;
        var previousPartyId = StepPartyId(step);
        valid = ValidateTarget(method, target, previousMethod, previousPartyId, previousPartyName);
        if (!valid.IsSuccess) return Result<DeliveryStepReassignment>.Failure(valid.ErrorMessage);

        var originalRank = step.StepOrder;
        var (oldShipment, oldTrip) = LatestStepRecord(step);

        Version++;
        RecordStep(step, DeliveryStatus.Cancelled, operation, reason, OrderHistorySource.ManualAction);
        CloseStepWorkRecord(step, WorkRecordStatus.Cancelled, reason, operation.AtUtc);

        OrderDeliveryStep replacement = null;
        if (method is Enums.DeliveryMethod.DeliveryMan or Enums.DeliveryMethod.ShippingCompany)
        {
            replacement = new OrderDeliveryStep
            {
                StepOrder = originalRank,
                DeliveryMethod = method,
                DeliverymanId = method == Enums.DeliveryMethod.DeliveryMan ? target.PartyId : null,
                ShippingCarrierId = method == Enums.DeliveryMethod.ShippingCompany ? target.PartyId : null
            };
            replacement.AttachTo(this);
            _deliverySteps.Add(replacement);
        }
        else
        {
            DeliveryMethod = Enums.DeliveryMethod.Pickup;
            DeliverymanId = null;
            ShippingCarrierId = null;
            foreach (var remaining in _deliverySteps.Where(item => !ReferenceEquals(item, step) && item.StepOrder > 0 &&
                         item.Status is not (DeliveryStatus.Completed or DeliveryStatus.Cancelled)))
            {
                RecordStep(remaining, DeliveryStatus.Cancelled, operation, reason, OrderHistorySource.ManualAction);
                CloseStepWorkRecord(remaining, WorkRecordStatus.Cancelled, reason, operation.AtUtc);
            }
            OrderStatus = OrderStatus.ReadyForPickup;
        }

        _deliveryMovements.Add(new OrderDeliveryMovement(this, step, replacement,
            oldShipment, oldTrip, null, null,
            previousMethod, previousPartyId, previousPartyName.Trim(),
            method, target?.PartyId, target?.Name?.Trim(), originalRank,
            operation, reason, Version));
        UpdateModificationDate(operation.AtUtc);
        return new DeliveryStepReassignment(step, replacement);
    }

    public Result<OrderDeliveryStep> CancelDeliveryStep(int stepId, string previousPartyName,
        OrderOperation operation, string reason)
    {
        var valid = ValidateOperation(operation);
        if (!valid.IsSuccess) return Result<OrderDeliveryStep>.Failure(valid.ErrorMessage);
        valid = ValidateReason(reason, true);
        if (!valid.IsSuccess) return Result<OrderDeliveryStep>.Failure(valid.ErrorMessage);
        if (IsArchived) return Result<OrderDeliveryStep>.Failure("الطلب مؤرشف ولا يقبل التشغيل.");
        if (IsFinal) return Result<OrderDeliveryStep>.Failure("الطلب مغلق ولا يقبل إلغاء الخطوات.");
        if (DeliveryMethod != Enums.DeliveryMethod.DeliveryChain)
            return Result<OrderDeliveryStep>.Failure("إلغاء الخطوات متاح لسلسلة التوصيل فقط.");

        var step = _deliverySteps.SingleOrDefault(item => item.Id == stepId);
        if (step is null) return Result<OrderDeliveryStep>.Failure("خطوة التوصيل غير موجودة في الطلب.");
        if (step.StepOrder <= 0) return Result<OrderDeliveryStep>.Failure("الخطوة ملغاة أو مستبدلة.");
        if (step.Status is not (DeliveryStatus.Pending or DeliveryStatus.InProgress or DeliveryStatus.Failed))
            return Result<OrderDeliveryStep>.Failure("لا يمكن إلغاء الخطوة من حالتها الحالية.");
        if (string.IsNullOrWhiteSpace(previousPartyName) || previousPartyName.Trim().Length > 100)
            return Result<OrderDeliveryStep>.Failure("بيانات جهة التوصيل الحالية غير صالحة.");
        if (_deliverySteps.Count(item => item.StepOrder > 0 && !ReferenceEquals(item, step) &&
                item.Status != DeliveryStatus.Cancelled) == 0)
            return Result<OrderDeliveryStep>.Failure("لا يمكن إلغاء آخر خطوة متبقية في سلسلة التوصيل.");

        var previousMethod = step.DeliveryMethod;
        var previousPartyId = StepPartyId(step);
        var originalRank = step.StepOrder;
        var (oldShipment, oldTrip) = LatestStepRecord(step);

        Version++;
        RecordStep(step, DeliveryStatus.Cancelled, operation, reason, OrderHistorySource.ManualAction);
        CloseStepWorkRecord(step, WorkRecordStatus.Cancelled, reason, operation.AtUtc);
        _deliveryMovements.Add(new OrderDeliveryMovement(this, step, null,
            oldShipment, oldTrip, null, null,
            previousMethod, previousPartyId, previousPartyName.Trim(),
            null, null, null, originalRank,
            operation, reason, Version, OrderHistorySource.DeliveryStepCancelled));
        UpdateModificationDate(operation.AtUtc);
        return Result<OrderDeliveryStep>.Success(step);
    }

    public Result ReassignDirectDelivery(DeliveryMethod method, DeliveryParty target, string previousPartyName,
        OrderOperation operation, string reason)
    {
        var valid = ValidateOperation(operation);
        if (!valid.IsSuccess) return valid;
        valid = ValidateReason(reason, true);
        if (!valid.IsSuccess) return valid;
        if (IsArchived) return Result.Failure("الطلب مؤرشف ولا يقبل التشغيل.");
        if (IsFinal) return Result.Failure("الطلب مغلق ولا يقبل إعادة التوجيه.");
        if (DeliveryMethod is not (Enums.DeliveryMethod.DeliveryMan or Enums.DeliveryMethod.ShippingCompany))
            return Result.Failure("إعادة التوجيه المباشر غير متاحة لهذا الطلب.");
        if (OrderStatus is not (OrderStatus.OutForDelivery or OrderStatus.Shipped or OrderStatus.DeliveryFailed))
            return Result.Failure("لا يمكن إعادة توجيه التكليف من الحالة الحالية.");

        var previousMethod = DeliveryMethod.Value;
        var previousPartyId = CurrentDirectPartyId();
        if (previousPartyId is not > 0) return Result.Failure("لا توجد جهة تنفيذ نشطة لإعادة توجيهها.");
        valid = ValidateTarget(method, target, previousMethod, previousPartyId, previousPartyName);
        if (!valid.IsSuccess) return valid;

        var (oldShipment, oldTrip) = LatestDirectRecord(previousMethod);

        Version++;
        CloseDirectWorkRecord(WorkRecordStatus.Cancelled, reason, operation.AtUtc);
        DeliveryMethod = method;
        DeliverymanId = method == Enums.DeliveryMethod.DeliveryMan ? target.PartyId : null;
        ShippingCarrierId = method == Enums.DeliveryMethod.ShippingCompany ? target.PartyId : null;
        if (method is Enums.DeliveryMethod.DeliveryMan or Enums.DeliveryMethod.ShippingCompany)
            StartDirectWorkRecord();
        OrderStatus = method switch
        {
            Enums.DeliveryMethod.DeliveryMan => OrderStatus.OutForDelivery,
            Enums.DeliveryMethod.ShippingCompany => OrderStatus.Shipped,
            _ => OrderStatus.ReadyForPickup
        };

        Shipment newShipment = null;
        DeliveryTrip newTrip = null;
        if (method == Enums.DeliveryMethod.ShippingCompany) newShipment = _shipments.LastOrDefault();
        else if (method == Enums.DeliveryMethod.DeliveryMan) newTrip = _deliveryTrips.LastOrDefault();

        _deliveryMovements.Add(new OrderDeliveryMovement(this, null, null,
            oldShipment, oldTrip, newShipment, newTrip,
            previousMethod, previousPartyId, previousPartyName.Trim(),
            method, target?.PartyId, target?.Name?.Trim(), null,
            operation, reason, Version));
        UpdateModificationDate(operation.AtUtc);
        return Result.Success();
    }

    private static Result ValidateTarget(DeliveryMethod method, DeliveryParty target, DeliveryMethod currentMethod,
        int? currentPartyId, string previousPartyName)
    {
        if (string.IsNullOrWhiteSpace(previousPartyName) || previousPartyName.Trim().Length > 100)
            return Result.Failure("بيانات جهة التوصيل الحالية غير صالحة.");
        if (!Enum.IsDefined(method)) return Result.Failure("طريقة التوصيل المطلوبة غير صالحة.");
        if (method == Enums.DeliveryMethod.Pickup)
            return target is null
                ? Result.Success()
                : Result.Failure("استلام من المقر لا يقبل جهة تنفيذ.");
        if (method is not (Enums.DeliveryMethod.DeliveryMan or Enums.DeliveryMethod.ShippingCompany))
            return Result.Failure("طريقة التوصيل المطلوبة غير صالحة.");
        if (target is null || target.PartyId <= 0 || string.IsNullOrWhiteSpace(target.Name) || target.Name.Trim().Length > 100)
            return Result.Failure("يجب تحديد جهة التوصيل.");
        if (method == currentMethod && target.PartyId == currentPartyId)
            return Result.Failure("يجب اختيار جهة توصيل مختلفة عن الجهة الحالية.");
        return Result.Success();
    }

    private static int? StepPartyId(OrderDeliveryStep step) => step.DeliveryMethod switch
    {
        Enums.DeliveryMethod.DeliveryMan => step.DeliverymanId,
        Enums.DeliveryMethod.ShippingCompany => step.ShippingCarrierId,
        _ => null
    };

    private int? CurrentDirectPartyId() => DeliveryMethod switch
    {
        Enums.DeliveryMethod.DeliveryMan => DeliverymanId,
        Enums.DeliveryMethod.ShippingCompany => ShippingCarrierId,
        _ => null
    };

    private static (Shipment Shipment, DeliveryTrip Trip) LatestStepRecord(OrderDeliveryStep step) =>
        step.DeliveryMethod == Enums.DeliveryMethod.ShippingCompany
            ? (step.Shipments.LastOrDefault(record => record.Status is WorkRecordStatus.Pending or WorkRecordStatus.Failed), null)
            : (null, step.DeliveryTrips.LastOrDefault(record => record.Status is WorkRecordStatus.Pending or WorkRecordStatus.Failed));

    private (Shipment Shipment, DeliveryTrip Trip) LatestDirectRecord(DeliveryMethod method) =>
        method == Enums.DeliveryMethod.ShippingCompany
            ? (_shipments.LastOrDefault(record => record.Status is WorkRecordStatus.Pending or WorkRecordStatus.Failed), null)
            : (null, _deliveryTrips.LastOrDefault(record => record.Status is WorkRecordStatus.Pending or WorkRecordStatus.Failed));
}
