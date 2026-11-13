using System;
using OrderHub.Domain.Common;

namespace OrderHub.Domain.Models;

/// <summary>
/// سجل تسليم كمية صُرفت من تخصيص مخزني واحد إلى العميل (D16/D19)، من مسار المشوار أو بدونه.
/// </summary>
public sealed class StockDeliveryRecord : ModelBase
{
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    private StockDeliveryRecord() { }

    private StockDeliveryRecord(
        int orderId,
        int orderItemId,
        int allocationId,
        int productId,
        int quantity,
        int? tripId,
        int? shipmentId,
        string? note,
        int recordedByUserId,
        DateTime effectiveAtUtc,
        DateTime nowUtc)
    {
        OrderId = orderId;
        OrderItemId = orderItemId;
        AllocationId = allocationId;
        ProductId = productId;
        Quantity = quantity;
        TripId = tripId;
        ShipmentId = shipmentId;
        Note = note;
        RecordedByUserId = recordedByUserId;
        EffectiveAtUtc = effectiveAtUtc;
        UpdateCreationDate(nowUtc);
    }

    public int OrderId { get; private set; }

    public Order Order { get; private set; }

    public int OrderItemId { get; private set; }

    public OrderItem OrderItem { get; private set; }

    public int AllocationId { get; private set; }

    public OrderInventoryAllocation Allocation { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; }

    public int Quantity { get; private set; }

    /// <summary>المشوار المرتبط بالتسليم عند التسليم عبر مسار التوصيل؛ فارغ للتسليم المباشر.</summary>
    public int? TripId { get; private set; }

    public DeliveryTrip Trip { get; private set; }

    public int? ShipmentId { get; private set; }

    public Shipment Shipment { get; private set; }

    /// <summary>ملاحظة حرة على التسليم (حتى 200 حرف)؛ تُخزَّن منظّفة أو فارغة.</summary>
    public string? Note { get; private set; }

    public int RecordedByUserId { get; private set; }

    public User RecordedBy { get; private set; }

    public DateTime EffectiveAtUtc { get; private set; }

    public static Result<StockDeliveryRecord> Create(
        int orderId,
        int orderItemId,
        int allocationId,
        int productId,
        int quantity,
        int? tripId,
        int? shipmentId,
        string? note,
        int recordedByUserId,
        DateTime effectiveAtUtc,
        DateTime nowUtc)
    {
        if (orderId <= 0 || orderItemId <= 0 || allocationId <= 0 || productId <= 0)
            return Result<StockDeliveryRecord>.Failure("بيانات تسليم المخزون غير صالحة.");

        if (quantity <= 0)
            return Result<StockDeliveryRecord>.Failure("كمية التسليم يجب أن تكون أكبر من صفر.");

        if (tripId is <= 0)
            return Result<StockDeliveryRecord>.Failure("المشوار المحدد غير صالح.");

        if (shipmentId is <= 0)
            return Result<StockDeliveryRecord>.Failure("الشحنة المحددة غير صالحة.");

        if (tripId.HasValue && shipmentId.HasValue)
            return Result<StockDeliveryRecord>.Failure("لا يمكن ربط التسليم بمشوار وشحنة في الوقت نفسه.");

        string? trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote is { Length: > 200 })
            return Result<StockDeliveryRecord>.Failure("ملاحظة التسليم يجب ألا تتجاوز 200 حرف.");

        if (recordedByUserId <= 0)
            return Result<StockDeliveryRecord>.Failure("الموظف المسجل للتسليم غير صالح.");

        DateTime effective = DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc);
        if (effective == default)
            return Result<StockDeliveryRecord>.Failure("وقت التسليم مطلوب.");

        if (effective > DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc).Add(FutureTolerance))
            return Result<StockDeliveryRecord>.Failure("لا يمكن أن يكون وقت التسليم في المستقبل.");

        return Result<StockDeliveryRecord>.Success(new StockDeliveryRecord(
            orderId,
            orderItemId,
            allocationId,
            productId,
            quantity,
            tripId,
            shipmentId,
            trimmedNote,
            recordedByUserId,
            effective,
            nowUtc));
    }
}
