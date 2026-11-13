using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

/// <summary>بند أمر الصرف المرتبط بتخصيص مخزني واحد.</summary>
public sealed class StockIssueLine : ModelBase
{
    private readonly List<StockIssueBatch> _batches = [];

    private StockIssueLine() { }

    internal StockIssueLine(int allocationId, int productId, int requestedQty)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(allocationId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestedQty);

        AllocationId = allocationId;
        ProductId = productId;
        RequestedQty = requestedQty;
    }

    public int IssueOrderId { get; private set; }

    public StockIssueOrder IssueOrder { get; private set; }

    public int AllocationId { get; private set; }

    public OrderInventoryAllocation Allocation { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; }

    public int RequestedQty { get; private set; }

    public IReadOnlyCollection<StockIssueBatch> Batches => _batches.AsReadOnly();

    public int IssuedQty => _batches.Sum(batch => batch.Quantity);

    public int RemainingQty => RequestedQty - IssuedQty;

    internal void AddBatch(
        int quantity,
        int recordedByUserId,
        DateTime effectiveAtUtc,
        string shortfallReason,
        DateTime nowUtc) =>
        _batches.Add(new StockIssueBatch(quantity, recordedByUserId, effectiveAtUtc, shortfallReason, nowUtc));
}
