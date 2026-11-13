using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Common;

namespace OrderHub.Domain.Models;

/// <summary>
/// مرتجع فعلي من عميل على طلب مصروف؛ يجمع الأسطر السليمة والتالفة ويُسجّل مرة واحدة لكل استلام.
/// </summary>
public sealed class InventoryReturn : ModelBase
{
    public const int NoteMaxLength = 500;
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    private readonly List<InventoryReturnLine> _lines = [];

    private InventoryReturn() { }

    public int WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    public int OrderId { get; private set; }

    public Order Order { get; private set; }

    /// <summary>أمر الصرف المرتبط بالمرتجع عند معرفته؛ فارغ للمرتجعات المباشرة.</summary>
    public int? StockIssueOrderId { get; private set; }

    public StockIssueOrder StockIssueOrder { get; private set; }

    public int RecordedByUserId { get; private set; }

    public User RecordedBy { get; private set; }

    public string Note { get; private set; }

    public DateTime EffectiveAtUtc { get; private set; }

    public IReadOnlyCollection<InventoryReturnLine> Lines => _lines.AsReadOnly();

    public static Result<InventoryReturn> Create(
        int warehouseId,
        int orderId,
        int? stockIssueOrderId,
        string note,
        int recordedByUserId,
        DateTime effectiveAtUtc,
        DateTime nowUtc)
    {
        if (warehouseId <= 0)
            return Result<InventoryReturn>.Failure("المخزن غير صالح.");

        if (orderId <= 0)
            return Result<InventoryReturn>.Failure("الطلب غير صالح.");

        if (stockIssueOrderId is <= 0)
            return Result<InventoryReturn>.Failure("أمر الصرف المرجعي غير صالح.");

        if (recordedByUserId <= 0)
            return Result<InventoryReturn>.Failure("الموظف المسجل غير صالح.");

        DateTime effective = DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc);
        if (effective == default)
            return Result<InventoryReturn>.Failure("وقت المرتجع مطلوب.");

        DateTime observedNow = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        if (effective > observedNow.Add(FutureTolerance))
            return Result<InventoryReturn>.Failure("لا يمكن أن يكون وقت المرتجع في المستقبل.");

        string trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote?.Length > NoteMaxLength)
            return Result<InventoryReturn>.Failure($"ملاحظة المرتجع لا تتجاوز {NoteMaxLength} حرف.");

        var record = new InventoryReturn
        {
            WarehouseId = warehouseId,
            OrderId = orderId,
            StockIssueOrderId = stockIssueOrderId,
            RecordedByUserId = recordedByUserId,
            Note = trimmedNote,
            EffectiveAtUtc = effective
        };
        record.UpdateCreationDate(observedNow);
        return Result<InventoryReturn>.Success(record);
    }

    public Result AddLine(InventoryReturnLine line)
    {
        if (line is null)
            return Result.Failure("سطر المرتجع غير صالح.");

        if (_lines.Any(existing => existing.AllocationId == line.AllocationId))
            return Result.Failure("لا يمكن تكرار نفس التخصيص في المرتجع الواحد.");

        line.AttachTo(this);
        _lines.Add(line);
        return Result.Success();
    }
}
