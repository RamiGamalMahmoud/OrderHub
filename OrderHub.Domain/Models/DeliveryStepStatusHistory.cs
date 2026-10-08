using System;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed class DeliveryStepStatusHistory
{
    private DeliveryStepStatusHistory() { }

    internal DeliveryStepStatusHistory(OrderDeliveryStep step, DeliveryStatus previous, DeliveryStatus next,
        OrderOperation operation, string reason, OrderHistorySource source, int version)
    {
        DeliveryStep = step;
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
    public int DeliveryStepId { get; private set; }
    public OrderDeliveryStep DeliveryStep { get; private set; }
    public DeliveryStatus? PreviousStatus { get; private set; }
    public DeliveryStatus NewStatus { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public int? ChangedByUserId { get; private set; }
    public string ChangedByUserName { get; private set; }
    public string Reason { get; private set; }
    public OrderHistorySource Source { get; private set; }
    public int OrderVersion { get; private set; }
}
