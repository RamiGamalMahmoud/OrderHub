using System;

namespace OrderHub.Domain.Models;

public class StockBalance : ModelBase
{
    private StockBalance() { }

    private StockBalance(int warehouseId, int productId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(warehouseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        WarehouseId = warehouseId;
        ProductId = productId;
    }

    public static StockBalance Create(int warehouseId, int productId) => new(warehouseId, productId);

    public int WarehouseId { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; }

    public int StockOnHand { get; private set; }

    public int ReservedOnHand { get; private set; }

    public int DamagedQty { get; private set; }

    public int Version { get; private set; }

    public int Available => StockOnHand - ReservedOnHand;

    public void ApplyIn(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        StockOnHand = checked(StockOnHand + quantity);
        Touch();
    }

    public void ApplyOut(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (StockOnHand - quantity < ReservedOnHand)
            throw new InvalidOperationException("لا يمكن صرف كمية تتجاوز المتاح بعد الكمية المحجوزة.");

        StockOnHand -= quantity;
        Touch();
    }

    public void DamagedIn(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        DamagedQty = checked(DamagedQty + quantity);
        Touch();
    }

    public void DamagedDisposal(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (quantity > DamagedQty)
            throw new InvalidOperationException("لا يمكن إتلاف كمية أكبر من الكمية التالفة المسجلة.");

        DamagedQty -= quantity;
        Touch();
    }

    public void DamagedAdjust(int delta)
    {
        if (delta == 0)
            throw new ArgumentOutOfRangeException(nameof(delta), "قيمة تسوية التالف يجب ألا تكون صفرًا.");

        if (DamagedQty + delta < 0)
            throw new InvalidOperationException("تسوية التالف تجعل الكمية التالفة سالبة.");

        DamagedQty += delta;
        Touch();
    }

    public void Reserve(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (quantity > Available)
            throw new InvalidOperationException("الكمية المتاحة لا تكفي للحجز.");

        ReservedOnHand = checked(ReservedOnHand + quantity);
        Touch();
    }

    public void Release(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (quantity > ReservedOnHand)
            throw new InvalidOperationException("لا يمكن تحرير كمية أكبر من الكمية المحجوزة.");

        ReservedOnHand -= quantity;
        Touch();
    }

    public void Consume(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (quantity > ReservedOnHand)
            throw new InvalidOperationException("لا يمكن صرف كمية أكبر من الكمية المحجوزة.");

        ReservedOnHand -= quantity;
        StockOnHand -= quantity;
        Touch();
    }

    /// <summary>
    /// Overwrites the cached quantities with the values derived from the ledger and active
    /// allocations (higher-manager rebuild, Q9). Returns false and leaves the row untouched
    /// when the values already match, so repeat runs stay idempotent.
    /// </summary>
    public bool Rebuild(int stockOnHand, int reservedOnHand, int damagedQty)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stockOnHand);
        ArgumentOutOfRangeException.ThrowIfNegative(reservedOnHand);
        ArgumentOutOfRangeException.ThrowIfNegative(damagedQty);

        if (reservedOnHand > stockOnHand)
            throw new InvalidOperationException("الكمية المحجوزة تتجاوز الرصيد بعد إعادة البناء.");

        if (StockOnHand == stockOnHand && ReservedOnHand == reservedOnHand && DamagedQty == damagedQty)
            return false;

        StockOnHand = stockOnHand;
        ReservedOnHand = reservedOnHand;
        DamagedQty = damagedQty;
        Touch();
        return true;
    }

    private void Touch()
    {
        Version++;
        UpdateModificationDate(DateTime.UtcNow);
    }
}
