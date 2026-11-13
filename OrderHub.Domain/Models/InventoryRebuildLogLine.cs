using System;
using OrderHub.Domain.Common;

namespace OrderHub.Domain.Models;

/// <summary>لقطة قبل/بعد لرصيد واحد داخل عملية إعادة بناء؛ تُسجَّل فقط عندما تتغيّر القيم.</summary>
public sealed class InventoryRebuildLogLine : ModelBase
{
    private InventoryRebuildLogLine() { }

    public int RebuildLogId { get; private set; }

    public InventoryRebuildLog RebuildLog { get; private set; }

    public int WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; }

    public int BeforeStockOnHand { get; private set; }

    public int BeforeReservedOnHand { get; private set; }

    public int BeforeDamagedQty { get; private set; }

    public int AfterStockOnHand { get; private set; }

    public int AfterReservedOnHand { get; private set; }

    public int AfterDamagedQty { get; private set; }

    internal static InventoryRebuildLogLine Create(
        int warehouseId,
        int productId,
        int beforeStockOnHand,
        int beforeReservedOnHand,
        int beforeDamagedQty,
        int afterStockOnHand,
        int afterReservedOnHand,
        int afterDamagedQty,
        DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(warehouseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);
        ArgumentOutOfRangeException.ThrowIfNegative(beforeStockOnHand);
        ArgumentOutOfRangeException.ThrowIfNegative(beforeReservedOnHand);
        ArgumentOutOfRangeException.ThrowIfNegative(beforeDamagedQty);
        ArgumentOutOfRangeException.ThrowIfNegative(afterStockOnHand);
        ArgumentOutOfRangeException.ThrowIfNegative(afterReservedOnHand);
        ArgumentOutOfRangeException.ThrowIfNegative(afterDamagedQty);

        var line = new InventoryRebuildLogLine
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            BeforeStockOnHand = beforeStockOnHand,
            BeforeReservedOnHand = beforeReservedOnHand,
            BeforeDamagedQty = beforeDamagedQty,
            AfterStockOnHand = afterStockOnHand,
            AfterReservedOnHand = afterReservedOnHand,
            AfterDamagedQty = afterDamagedQty
        };
        line.UpdateCreationDate(nowUtc);
        return line;
    }

    internal void AttachTo(InventoryRebuildLog log)
    {
        if (Id > 0 && RebuildLog is not null && !ReferenceEquals(RebuildLog, log))
            throw new InvalidOperationException("لا يمكن نقل سطر تدقيق محفوظ إلى عملية أخرى.");

        RebuildLog = log;
        RebuildLogId = log.Id;
    }
}
