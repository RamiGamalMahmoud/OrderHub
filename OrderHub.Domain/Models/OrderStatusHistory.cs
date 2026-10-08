using System;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed class OrderStatusHistory
{
    private OrderStatusHistory() { }

    internal OrderStatusHistory(OrderStatus? previous, OrderStatus next, OrderOperation operation,
        string reason, OrderHistorySource source, int version)
    {
        PreviousStatus = previous;
        NewStatus = next;
        ChangedAtUtc = operation.AtUtc;
        ChangedByUserId = operation.UserId;
        ChangedByUserName = operation.UserName.Trim();
        Reason = reason?.Trim();
        Source = source;
        OrderVersion = version;
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public Order Order { get; private set; }
    public OrderStatus? PreviousStatus { get; private set; }
    public OrderStatus NewStatus { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public int? ChangedByUserId { get; private set; }
    public string ChangedByUserName { get; private set; }
    public string Reason { get; private set; }
    public OrderHistorySource Source { get; private set; }
    public int OrderVersion { get; private set; }
    public string OriginalStatus { get; private set; }
}
