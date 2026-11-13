using System;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed class InventoryMovement
{
    private const int NoteMaxLength = 500;
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    private InventoryMovement() { }

    public int Id { get; private set; }

    public int WarehouseId { get; private set; }

    public int ProductId { get; private set; }

    public int Quantity { get; private set; }

    public InventoryDirection Direction { get; private set; }

    public InventoryMovementType MovementType { get; private set; }

    public int? SourceStockReceiptId { get; private set; }

    public int? SourceIssueBatchId { get; private set; }

    public int? SourceReturnId { get; private set; }

    public int? SourceCountId { get; private set; }

    public int? SourceDeliveryRecordId { get; private set; }

    public int? ReversalOfMovementId { get; private set; }

    public InventoryMovementStatus Status { get; private set; }

    public DateTime EffectiveAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public int CreatedByUserId { get; private set; }

    public string Note { get; private set; }

    public static InventoryMovement Create(
        int warehouseId,
        int productId,
        int quantity,
        InventoryDirection direction,
        InventoryMovementType movementType,
        DateTime effectiveAtUtc,
        int createdByUserId,
        string note = null,
        int? reversalOfMovementId = null,
        int? sourceStockReceiptId = null,
        int? sourceIssueBatchId = null,
        int? sourceReturnId = null,
        int? sourceCountId = null,
        int? sourceDeliveryRecordId = null,
        DateTime? nowUtc = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(warehouseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId);

        ValidateOptionalReference(sourceStockReceiptId, nameof(sourceStockReceiptId));
        ValidateOptionalReference(sourceIssueBatchId, nameof(sourceIssueBatchId));
        ValidateOptionalReference(sourceReturnId, nameof(sourceReturnId));
        ValidateOptionalReference(sourceCountId, nameof(sourceCountId));
        ValidateOptionalReference(sourceDeliveryRecordId, nameof(sourceDeliveryRecordId));

        if (!IsDirectionAllowed(movementType, direction))
            throw new ArgumentException("اتجاه الحركة لا يطابق نوعها.", nameof(direction));

        if (movementType == InventoryMovementType.Reversal)
        {
            if (reversalOfMovementId is null or <= 0)
                throw new ArgumentException("حركة العكس تتطلب حركة أصل صحيحة.", nameof(reversalOfMovementId));
        }
        else if (reversalOfMovementId is not null)
        {
            throw new ArgumentException("حركة الأصل لا تُربط بحركة عكس.", nameof(reversalOfMovementId));
        }

        if (effectiveAtUtc == default)
            throw new ArgumentException("وقت الحركة مطلوب.", nameof(effectiveAtUtc));

        var effective = DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc);
        var observedNow = DateTime.SpecifyKind(nowUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        if (effective > observedNow.Add(FutureTolerance))
            throw new ArgumentOutOfRangeException(nameof(effectiveAtUtc), "لا يمكن أن يكون وقت الحركة في المستقبل.");

        var trimmedNote = note?.Trim();
        if (trimmedNote?.Length > NoteMaxLength)
            throw new ArgumentOutOfRangeException(nameof(note), "ملاحظة الحركة تتجاوز 500 حرف.");

        if (RequiresNote(movementType) && string.IsNullOrWhiteSpace(trimmedNote))
            throw new ArgumentException("الملاحظة إلزامية لهذا النوع من الحركات.", nameof(note));

        return new InventoryMovement
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            Direction = direction,
            MovementType = movementType,
            SourceStockReceiptId = sourceStockReceiptId,
            SourceIssueBatchId = sourceIssueBatchId,
            SourceReturnId = sourceReturnId,
            SourceCountId = sourceCountId,
            SourceDeliveryRecordId = sourceDeliveryRecordId,
            ReversalOfMovementId = reversalOfMovementId,
            Status = InventoryMovementStatus.Active,
            EffectiveAtUtc = effective,
            CreatedAtUtc = observedNow,
            CreatedByUserId = createdByUserId,
            Note = trimmedNote
        };
    }

    public Result MarkReversed()
    {
        if (Status != InventoryMovementStatus.Active)
            return Result.Failure("لا يمكن عكس حركة غير نشطة.", "Conflict");

        Status = InventoryMovementStatus.Reversed;
        return Result.Success();
    }

    private static void ValidateOptionalReference(int? value, string parameterName)
    {
        if (value is <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "المعرّف المرجعي يجب أن يكون أكبر من صفر.");
    }

    private static bool IsDirectionAllowed(InventoryMovementType movementType, InventoryDirection direction) =>
        movementType switch
        {
            InventoryMovementType.OpeningStock or InventoryMovementType.StockIn
                or InventoryMovementType.ReturnIn or InventoryMovementType.ReturnDamagedIn =>
                direction == InventoryDirection.In,
            InventoryMovementType.IssueOut or InventoryMovementType.DamagedDisposal =>
                direction == InventoryDirection.Out,
            InventoryMovementType.Adjustment or InventoryMovementType.DamagedAdjustment
                or InventoryMovementType.Reversal =>
                direction is InventoryDirection.In or InventoryDirection.Out,
            _ => false
        };

    private static bool RequiresNote(InventoryMovementType movementType) =>
        movementType is InventoryMovementType.Reversal
            or InventoryMovementType.Adjustment
            or InventoryMovementType.DamagedAdjustment;
}
