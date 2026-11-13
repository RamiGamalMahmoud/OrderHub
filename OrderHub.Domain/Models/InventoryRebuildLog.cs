using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

/// <summary>
/// سجل تدقيق لعملية إعادة بناء أرصدة يدوية يقودها المدير الأعلى؛ يحفظ من قام بها ومتى
/// وقبل/بعد كل رصيد تغيّر. الأرصدة مشتقة من الدفتر، لذلك لا تُنشئ إعادة البناء أي حركة.
/// </summary>
public sealed class InventoryRebuildLog : ModelBase
{
    private readonly List<InventoryRebuildLogLine> _lines = [];

    private InventoryRebuildLog() { }

    public int PerformedByUserId { get; private set; }

    public User PerformedBy { get; private set; }

    public InventoryRebuildScope Scope { get; private set; }

    /// <summary>المخزن المستهدف في نطاق المخزن الواحد؛ فارغ في نطاق كل المخازن.</summary>
    public int? WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    public int InspectedPairs { get; private set; }

    public int ChangedPairs { get; private set; }

    public IReadOnlyCollection<InventoryRebuildLogLine> Lines => _lines.AsReadOnly();

    public static Result<InventoryRebuildLog> Create(
        InventoryRebuildScope scope,
        int? warehouseId,
        int performedByUserId,
        int inspectedPairs,
        int changedPairs,
        DateTime nowUtc)
    {
        if (!Enum.IsDefined(scope))
            return Result<InventoryRebuildLog>.Failure("نطاق إعادة البناء غير صالح.");

        if (scope == InventoryRebuildScope.SingleWarehouse && warehouseId is not > 0)
            return Result<InventoryRebuildLog>.Failure("يجب تحديد المخزن المطلوب إعادة بناء أرصدته.");

        if (scope == InventoryRebuildScope.AllWarehouses && warehouseId is not null)
            return Result<InventoryRebuildLog>.Failure("نطاق كل المخازن لا يقبل مخزنًا محددًا.");

        if (performedByUserId <= 0)
            return Result<InventoryRebuildLog>.Failure("الموظف المسجل غير صالح.");

        if (inspectedPairs < 0 || changedPairs < 0 || changedPairs > inspectedPairs)
            return Result<InventoryRebuildLog>.Failure("أعداد أزواج الأرصدة غير صالحة.");

        var log = new InventoryRebuildLog
        {
            Scope = scope,
            WarehouseId = warehouseId,
            PerformedByUserId = performedByUserId,
            InspectedPairs = inspectedPairs,
            ChangedPairs = changedPairs
        };
        log.UpdateCreationDate(nowUtc);
        return Result<InventoryRebuildLog>.Success(log);
    }

    public Result<InventoryRebuildLogLine> AddLine(
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
        if (Scope == InventoryRebuildScope.SingleWarehouse && warehouseId != WarehouseId)
            return Result<InventoryRebuildLogLine>.Failure("سطر التدقيق خارج نطاق إعادة البناء.");

        if (_lines.Any(line => line.WarehouseId == warehouseId && line.ProductId == productId))
            return Result<InventoryRebuildLogLine>.Failure("لا يمكن تكرار الرصيد في سجل إعادة البناء نفسه.");

        InventoryRebuildLogLine line = InventoryRebuildLogLine.Create(
            warehouseId,
            productId,
            beforeStockOnHand,
            beforeReservedOnHand,
            beforeDamagedQty,
            afterStockOnHand,
            afterReservedOnHand,
            afterDamagedQty,
            nowUtc);
        line.AttachTo(this);
        _lines.Add(line);
        return Result<InventoryRebuildLogLine>.Success(line);
    }
}
