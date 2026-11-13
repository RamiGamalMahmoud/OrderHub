using System;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

/// <summary>
/// طلب اعتماد لعملية حساسة على المخزون؛ القرار واحد فقط ويُحمى بإصدار متزايد.
/// </summary>
public sealed class InventoryApprovalRequest : ModelBase
{
    public const int ReasonMaxLength = 200;
    public const int DecisionNoteMaxLength = 200;

    private InventoryApprovalRequest() { }

    public InventoryApprovalRequestType RequestType { get; private set; }

    public InventoryApprovalStatus Status { get; private set; }

    public int? OrderId { get; private set; }

    public Order Order { get; private set; }

    public int? StockIssueOrderId { get; private set; }

    public StockIssueOrder StockIssueOrder { get; private set; }

    public int? InventoryMovementId { get; private set; }

    public InventoryMovement InventoryMovement { get; private set; }

    /// <summary>مخزن الإتلاف المطلوب (نوع `DamagedDisposal` فقط).</summary>
    public int? WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; }

    /// <summary>الصنف المطلوب إتلافه (نوع `DamagedDisposal` فقط).</summary>
    public int? ProductId { get; private set; }

    public Product Product { get; private set; }

    /// <summary>كمية الإتلاف المطلوبة من رصيد التالف (نوع `DamagedDisposal` فقط).</summary>
    public int? DisposalQuantity { get; private set; }

    /// <summary>الحمولة الكاملة المقترحة للتعديل (JSON)؛ تُطبَّق ذريًا عند الاعتماد.</summary>
    public string Payload { get; private set; }

    public string Reason { get; private set; }

    /// <summary>إصدار الهدف وقت الطلب؛ أي انحراف يحوّل الطلب إلى «متقادم».</summary>
    public int? TargetVersion { get; private set; }

    public int Version { get; private set; }

    public int RequestedByUserId { get; private set; }

    public User RequestedBy { get; private set; }

    public int? DecidedByUserId { get; private set; }

    public User DecidedBy { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public string DecisionNote { get; private set; }

    public static Result<InventoryApprovalRequest> CreateOrderEdit(
        int orderId,
        int targetVersion,
        string payload,
        string reason,
        int requestedByUserId,
        DateTime nowUtc)
    {
        if (orderId <= 0)
            return Result<InventoryApprovalRequest>.Failure("الطلب غير صالح.");
        if (targetVersion <= 0)
            return Result<InventoryApprovalRequest>.Failure("إصدار الطلب غير صالح.");
        if (string.IsNullOrWhiteSpace(payload))
            return Result<InventoryApprovalRequest>.Failure("حمولة التعديل مطلوبة.");

        Result<string> normalized = NormalizeReason(reason);
        if (!normalized.IsSuccess)
            return Result<InventoryApprovalRequest>.Failure(normalized.ErrorMessage);

        var request = new InventoryApprovalRequest
        {
            RequestType = InventoryApprovalRequestType.AllocationEdit,
            Status = InventoryApprovalStatus.Pending,
            OrderId = orderId,
            Payload = payload,
            Reason = normalized.Value,
            TargetVersion = targetVersion,
            RequestedByUserId = ValidateRequester(requestedByUserId)
        };
        request.UpdateCreationDate(nowUtc);
        return Result<InventoryApprovalRequest>.Success(request);
    }

    public static Result<InventoryApprovalRequest> CreateIssueOrderCancel(
        int issueOrderId,
        int targetVersion,
        string reason,
        int requestedByUserId,
        DateTime nowUtc)
    {
        if (issueOrderId <= 0)
            return Result<InventoryApprovalRequest>.Failure("أمر الصرف غير صالح.");
        if (targetVersion < 0)
            return Result<InventoryApprovalRequest>.Failure("إصدار أمر الصرف غير صالح.");

        Result<string> normalized = NormalizeReason(reason);
        if (!normalized.IsSuccess)
            return Result<InventoryApprovalRequest>.Failure(normalized.ErrorMessage);

        var request = new InventoryApprovalRequest
        {
            RequestType = InventoryApprovalRequestType.IssueOrderCancel,
            Status = InventoryApprovalStatus.Pending,
            StockIssueOrderId = issueOrderId,
            Reason = normalized.Value,
            TargetVersion = targetVersion,
            RequestedByUserId = ValidateRequester(requestedByUserId)
        };
        request.UpdateCreationDate(nowUtc);
        return Result<InventoryApprovalRequest>.Success(request);
    }

    public static Result<InventoryApprovalRequest> CreateMovementReversal(
        int movementId,
        string reason,
        int requestedByUserId,
        DateTime nowUtc)
    {
        if (movementId <= 0)
            return Result<InventoryApprovalRequest>.Failure("الحركة غير صالحة.");

        Result<string> normalized = NormalizeReason(reason);
        if (!normalized.IsSuccess)
            return Result<InventoryApprovalRequest>.Failure(normalized.ErrorMessage);

        var request = new InventoryApprovalRequest
        {
            RequestType = InventoryApprovalRequestType.MovementReversal,
            Status = InventoryApprovalStatus.Pending,
            InventoryMovementId = movementId,
            Reason = normalized.Value,
            RequestedByUserId = ValidateRequester(requestedByUserId)
        };
        request.UpdateCreationDate(nowUtc);
        return Result<InventoryApprovalRequest>.Success(request);
    }

    public static Result<InventoryApprovalRequest> CreateDamagedDisposal(
        int warehouseId,
        int productId,
        int quantity,
        int balanceVersion,
        string reason,
        int requestedByUserId,
        DateTime nowUtc)
    {
        if (warehouseId <= 0 || productId <= 0)
            return Result<InventoryApprovalRequest>.Failure("مخزن أو صنف الإتلاف غير صالح.");

        if (quantity <= 0)
            return Result<InventoryApprovalRequest>.Failure("كمية الإتلاف يجب أن تكون أكبر من صفر.");

        if (balanceVersion < 0)
            return Result<InventoryApprovalRequest>.Failure("إصدار الرصيد غير صالح.");

        Result<string> normalized = NormalizeReason(reason);
        if (!normalized.IsSuccess)
            return Result<InventoryApprovalRequest>.Failure(normalized.ErrorMessage);

        var request = new InventoryApprovalRequest
        {
            RequestType = InventoryApprovalRequestType.DamagedDisposal,
            Status = InventoryApprovalStatus.Pending,
            WarehouseId = warehouseId,
            ProductId = productId,
            DisposalQuantity = quantity,
            Reason = normalized.Value,
            TargetVersion = balanceVersion,
            RequestedByUserId = ValidateRequester(requestedByUserId)
        };
        request.UpdateCreationDate(nowUtc);
        return Result<InventoryApprovalRequest>.Success(request);
    }

    /// <summary>Single decision; the repository guards it with a version CAS.</summary>
    public Result Approve(int deciderUserId, string note, DateTime nowUtc)
    {
        if (Status != InventoryApprovalStatus.Pending)
            return Result.Failure("تمت معالجة هذا الطلب مسبقًا.", "Conflict");

        Result<string> normalized = NormalizeNote(note);
        if (!normalized.IsSuccess)
            return Result.Failure(normalized.ErrorMessage);

        Status = InventoryApprovalStatus.Approved;
        DecidedByUserId = ValidateDecider(deciderUserId);
        DecidedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        DecisionNote = normalized.Value ?? "تمت الموافقة.";
        TouchDecision(nowUtc);
        return Result.Success();
    }

    public Result Reject(int deciderUserId, string note, DateTime nowUtc)
    {
        if (Status != InventoryApprovalStatus.Pending)
            return Result.Failure("تمت معالجة هذا الطلب مسبقًا.", "Conflict");

        Result<string> normalized = NormalizeNote(note);
        if (!normalized.IsSuccess || string.IsNullOrWhiteSpace(normalized.Value))
            return Result.Failure("سبب الرفض مطلوب.");

        Status = InventoryApprovalStatus.Rejected;
        DecidedByUserId = ValidateDecider(deciderUserId);
        DecidedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        DecisionNote = normalized.Value;
        TouchDecision(nowUtc);
        return Result.Success();
    }

    /// <summary>Moves a pending request to «متقادم» when its target drifted; no side effects.</summary>
    public Result MarkStale(string note, DateTime nowUtc)
    {
        if (Status != InventoryApprovalStatus.Pending)
            return Result.Failure("تمت معالجة هذا الطلب مسبقًا.", "Conflict");

        Status = InventoryApprovalStatus.Stale;
        DecisionNote = string.IsNullOrWhiteSpace(note)
            ? "تغيّر الهدف بعد إرسال الطلب؛ أعد الإرسال."
            : note.Trim();
        DecidedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        TouchDecision(nowUtc);
        return Result.Success();
    }

    private void TouchDecision(DateTime nowUtc)
    {
        Version++;
        UpdateModificationDate(nowUtc);
    }

    private static Result<string> NormalizeReason(string reason)
    {
        string trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result<string>.Failure("سبب الطلب مطلوب.");
        if (trimmed.Length > ReasonMaxLength)
            return Result<string>.Failure($"سبب الطلب لا يتجاوز {ReasonMaxLength} حرف.");
        return Result<string>.Success(trimmed);
    }

    private static Result<string> NormalizeNote(string note)
    {
        string trimmed = note?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result<string>.Success(null);
        if (trimmed.Length > DecisionNoteMaxLength)
            return Result<string>.Failure($"ملاحظة القرار لا تتجاوز {DecisionNoteMaxLength} حرف.");
        return Result<string>.Success(trimmed);
    }

    private static int ValidateRequester(int userId) =>
        userId > 0 ? userId : throw new ArgumentOutOfRangeException(nameof(userId));

    private static int ValidateDecider(int userId) =>
        userId > 0 ? userId : throw new ArgumentOutOfRangeException(nameof(userId));
}
