using System;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

/// <summary>
/// سطر جرد لصنف واحد؛ يحتفظ بلقطة دفترية سليمة وتالفة ونتيجة العدّ الفعلي.
/// </summary>
public sealed class InventoryCountLine : ModelBase
{
    public const int ReviewNoteMaxLength = 200;

    private InventoryCountLine() { }

    public int CountId { get; private set; }

    public InventoryCount Count { get; private set; }

    /// <summary>يُكرَّر على السطر لتفرض قاعدة الفهرس الفريد جردًا واحدًا نشطًا لكل (مخزن، صنف).</summary>
    public int WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; }

    public int BookQty { get; private set; }

    public int BookDamagedQty { get; private set; }

    public int? CountedQty { get; private set; }

    public int? CountedDamagedQty { get; private set; }

    public DateTime SnapshotAtUtc { get; private set; }

    /// <summary>لحظة حساب اللقطة؛ أساس كشف الحركات التي سُجلت بعدها بتاريخ سريان أقدم.</summary>
    public DateTime SnapshotRecordedAtUtc { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public LateMovementDecision? ReviewDecision { get; private set; }

    public string ReviewNote { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsCounted => CountedQty is not null && CountedDamagedQty is not null;

    public int? Delta => CountedQty is int counted ? counted - BookQty : null;

    public int? DamagedDelta => CountedDamagedQty is int counted ? counted - BookDamagedQty : null;

    internal static InventoryCountLine Create(
        int warehouseId,
        int productId,
        int bookQty,
        int bookDamagedQty,
        DateTime snapshotAtUtc,
        DateTime nowUtc)
    {
        var line = new InventoryCountLine
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            BookQty = bookQty,
            BookDamagedQty = bookDamagedQty,
            SnapshotAtUtc = DateTime.SpecifyKind(snapshotAtUtc, DateTimeKind.Utc),
            SnapshotRecordedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            IsActive = true
        };
        line.UpdateCreationDate(nowUtc);
        return line;
    }

    internal void AttachTo(InventoryCount count)
    {
        Count = count;
        CountId = count.Id;
    }

    internal Result SetCounted(int? countedQty, int? countedDamagedQty)
    {
        if (countedQty is < 0 || countedDamagedQty is < 0)
            return Result.Failure("كميات العدّ لا تكون سالبة.");

        CountedQty = countedQty;
        CountedDamagedQty = countedDamagedQty;
        return Result.Success();
    }

    internal void IncludeInCount(int bookQty, int bookDamagedQty, string note, DateTime nowUtc)
    {
        BookQty = bookQty;
        BookDamagedQty = bookDamagedQty;
        ReviewDecision = LateMovementDecision.IncludedInCount;
        ReviewNote = note;
        ReviewedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        UpdateModificationDate(nowUtc);
    }

    internal void Recount(int bookQty, int bookDamagedQty, string note, DateTime nowUtc)
    {
        BookQty = bookQty;
        BookDamagedQty = bookDamagedQty;
        CountedQty = null;
        CountedDamagedQty = null;
        SnapshotAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        SnapshotRecordedAtUtc = SnapshotAtUtc;
        ReviewedAtUtc = SnapshotAtUtc;
        ReviewDecision = LateMovementDecision.Recounted;
        ReviewNote = note;
        UpdateModificationDate(nowUtc);
    }

    internal void Deactivate() => IsActive = false;
}
