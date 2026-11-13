using System;

namespace OrderHub.Domain.Models;

/// <summary>دفعة صرف فعلية من بند أمر صرف؛ كل دفعة تقابلها حركة صرف واحدة.</summary>
public sealed class StockIssueBatch : ModelBase
{
    public const int ReasonMaxLength = 300;

    private StockIssueBatch() { }

    internal StockIssueBatch(
        int quantity,
        int recordedByUserId,
        DateTime effectiveAtUtc,
        string shortfallReason,
        DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recordedByUserId);
        if (effectiveAtUtc == default)
            throw new ArgumentException("وقت الصرف مطلوب.", nameof(effectiveAtUtc));

        string trimmedReason = shortfallReason?.Trim();
        if (string.IsNullOrEmpty(trimmedReason))
            trimmedReason = null;
        if (trimmedReason?.Length > ReasonMaxLength)
            throw new ArgumentOutOfRangeException(nameof(shortfallReason), "سبب النقص لا يتجاوز 300 حرف.");

        Quantity = quantity;
        RecordedByUserId = recordedByUserId;
        EffectiveAtUtc = DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc);
        ShortfallReason = trimmedReason;
        UpdateCreationDate(nowUtc);
    }

    public int IssueOrderLineId { get; private set; }

    public StockIssueLine IssueOrderLine { get; private set; }

    public int Quantity { get; private set; }

    public int RecordedByUserId { get; private set; }

    public User RecordedBy { get; private set; }

    public DateTime EffectiveAtUtc { get; private set; }

    public string ShortfallReason { get; private set; }
}
