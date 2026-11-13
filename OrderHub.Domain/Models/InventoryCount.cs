using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

/// <summary>
/// جرد مخزني يقوده أمين المخزن ويعتمده المدير؛ الأسطر تلتقط لقطة دفترية وقت البدء
/// والتسويات تُرحَّل فقط عند الاعتماد.
/// </summary>
public sealed class InventoryCount : ModelBase
{
    public const int DecisionNoteMaxLength = 200;

    private readonly List<InventoryCountLine> _lines = [];

    private InventoryCount() { }

    public int WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    public InventoryCountScope Scope { get; private set; }

    public InventoryCountStatus Status { get; private set; }

    /// <summary>حدّ اللقطة الزمني؛ تُحسب الكميات الدفترية من الحركات ذات وقت سريان ≤ هذه اللحظة.</summary>
    public DateTime SnapshotAtUtc { get; private set; }

    public int Version { get; private set; }

    public int StartedByUserId { get; private set; }

    public User StartedBy { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public int? DecidedByUserId { get; private set; }

    public User DecidedBy { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public string DecisionNote { get; private set; }

    public IReadOnlyCollection<InventoryCountLine> Lines => _lines.AsReadOnly();

    public static Result<InventoryCount> Start(
        int warehouseId,
        InventoryCountScope scope,
        int startedByUserId,
        DateTime nowUtc)
    {
        if (warehouseId <= 0)
            return Result<InventoryCount>.Failure("المخزن غير صالح.");

        if (startedByUserId <= 0)
            return Result<InventoryCount>.Failure("الموظف المسجل غير صالح.");

        if (!Enum.IsDefined(scope))
            return Result<InventoryCount>.Failure("نطاق الجرد غير صالح.");

        var count = new InventoryCount
        {
            WarehouseId = warehouseId,
            Scope = scope,
            Status = InventoryCountStatus.Open,
            SnapshotAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            Version = 0,
            StartedByUserId = startedByUserId
        };
        count.UpdateCreationDate(nowUtc);
        return Result<InventoryCount>.Success(count);
    }

    public Result<InventoryCountLine> AddLine(
        int productId,
        int bookQty,
        int bookDamagedQty,
        DateTime nowUtc)
    {
        if (Status != InventoryCountStatus.Open)
            return Result<InventoryCountLine>.Failure("لا يمكن إضافة أسطر لجرد غير مفتوح.", "Conflict");

        if (productId <= 0)
            return Result<InventoryCountLine>.Failure("الصنف غير صالح.");

        if (_lines.Any(line => line.ProductId == productId))
            return Result<InventoryCountLine>.Failure("لا يمكن تكرار الصنف في الجرد الواحد.");

        InventoryCountLine line = InventoryCountLine.Create(
            WarehouseId,
            productId,
            bookQty,
            bookDamagedQty,
            SnapshotAtUtc,
            nowUtc);
        line.AttachTo(this);
        _lines.Add(line);
        return Result<InventoryCountLine>.Success(line);
    }

    public Result SaveCounted(
        int lineId,
        int? countedQty,
        int? countedDamagedQty,
        DateTime nowUtc)
    {
        if (Status != InventoryCountStatus.Open)
            return Result.Failure("لا يمكن تعديل جرد غير مفتوح.", "Conflict");

        InventoryCountLine line = FindLine(lineId);
        if (line is null)
            return Result.Failure("سطر الجرد غير موجود.", "NotFound");

        Result saved = line.SetCounted(countedQty, countedDamagedQty);
        if (!saved.IsSuccess)
            return saved;

        Version++;
        UpdateModificationDate(nowUtc);
        return Result.Success();
    }

    public Result Complete(DateTime nowUtc)
    {
        if (Status != InventoryCountStatus.Open)
            return Result.Failure("الجرد ليس مفتوحًا لإنهاء العدّ.", "Conflict");

        if (_lines.Count == 0)
            return Result.Failure("أضف صنفًا واحدًا على الأقل قبل إنهاء الجرد.");

        InventoryCountLine missing = _lines.FirstOrDefault(line => line.IsActive && !line.IsCounted);
        if (missing is not null)
            return Result.Failure("أدخل الكمية السليمة والتالفة لكل صنف قبل إنهاء الجرد.", "Conflict");

        Status = InventoryCountStatus.UnderReview;
        CompletedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        Version++;
        UpdateModificationDate(nowUtc);
        return Result.Success();
    }

    public Result IncludeLateMovement(
        int lineId,
        int bookQty,
        int bookDamagedQty,
        string note,
        DateTime nowUtc)
    {
        if (Status != InventoryCountStatus.UnderReview)
            return Result.Failure("مراجعة الحركات المتأخرة متاحة بعد إنهاء العدّ فقط.", "Conflict");

        InventoryCountLine line = FindLine(lineId);
        if (line is null)
            return Result.Failure("سطر الجرد غير موجود.", "NotFound");

        Result<string> normalized = NormalizeReviewNote(note);
        if (!normalized.IsSuccess)
            return normalized;

        line.IncludeInCount(bookQty, bookDamagedQty, normalized.Value, nowUtc);
        Version++;
        UpdateModificationDate(nowUtc);
        return Result.Success();
    }

    public Result Recount(
        int lineId,
        int bookQty,
        int bookDamagedQty,
        string note,
        DateTime nowUtc)
    {
        if (Status != InventoryCountStatus.UnderReview)
            return Result.Failure("إعادة العدّ متاحة بعد إنهاء العدّ فقط.", "Conflict");

        InventoryCountLine line = FindLine(lineId);
        if (line is null)
            return Result.Failure("سطر الجرد غير موجود.", "NotFound");

        Result<string> normalized = NormalizeReviewNote(note);
        if (!normalized.IsSuccess)
            return normalized;

        line.Recount(bookQty, bookDamagedQty, normalized.Value, nowUtc);

        // The recounted line must be counted again, so the whole count reopens.
        Status = InventoryCountStatus.Open;
        CompletedAtUtc = null;
        Version++;
        UpdateModificationDate(nowUtc);
        return Result.Success();
    }

    public Result Approve(int userId, string note, DateTime nowUtc)
    {
        if (Status != InventoryCountStatus.UnderReview)
            return Result.Failure("لا يمكن اعتماد جرد لم يُنهَ عدّه.", "Conflict");

        InventoryCountLine missing = _lines.FirstOrDefault(line => line.IsActive && !line.IsCounted);
        if (missing is not null)
            return Result.Failure("لا يمكن الاعتماد قبل إدخال الكمية السليمة والتالفة لكل صنف.", "Conflict");

        Result<string> normalized = NormalizeDecisionNote(note);
        if (!normalized.IsSuccess)
            return normalized;

        Status = InventoryCountStatus.Approved;
        DecidedByUserId = ValidateUserId(userId);
        DecidedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        DecisionNote = normalized.Value ?? "تم اعتماد الجرد.";
        DeactivateLines();
        Version++;
        UpdateModificationDate(nowUtc);
        return Result.Success();
    }

    public Result Cancel(int userId, string note, DateTime nowUtc)
    {
        if (Status is not (InventoryCountStatus.Open or InventoryCountStatus.UnderReview))
            return Result.Failure("لا يمكن إلغاء جرد مغلق.", "Conflict");

        Result<string> normalized = NormalizeDecisionNote(note);
        if (!normalized.IsSuccess || string.IsNullOrWhiteSpace(normalized.Value))
            return Result.Failure("سبب الإلغاء مطلوب.");

        Status = InventoryCountStatus.Cancelled;
        DecidedByUserId = ValidateUserId(userId);
        DecidedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        DecisionNote = normalized.Value;
        DeactivateLines();
        Version++;
        UpdateModificationDate(nowUtc);
        return Result.Success();
    }

    private InventoryCountLine FindLine(int lineId) =>
        _lines.FirstOrDefault(line => line.Id == lineId && line.IsActive);

    private void DeactivateLines()
    {
        foreach (InventoryCountLine line in _lines)
            line.Deactivate();
    }

    private static Result<string> NormalizeReviewNote(string note)
    {
        string trimmed = note?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result<string>.Success(null);
        return trimmed.Length > InventoryCountLine.ReviewNoteMaxLength
            ? Result<string>.Failure($"ملاحظة المراجعة لا تتجاوز {InventoryCountLine.ReviewNoteMaxLength} حرف.")
            : Result<string>.Success(trimmed);
    }

    private static Result<string> NormalizeDecisionNote(string note)
    {
        string trimmed = note?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result<string>.Success(null);
        return trimmed.Length > DecisionNoteMaxLength
            ? Result<string>.Failure($"ملاحظة الجرد لا تتجاوز {DecisionNoteMaxLength} حرف.")
            : Result<string>.Success(trimmed);
    }

    private static int ValidateUserId(int userId) =>
        userId > 0 ? userId : throw new ArgumentOutOfRangeException(nameof(userId));
}
