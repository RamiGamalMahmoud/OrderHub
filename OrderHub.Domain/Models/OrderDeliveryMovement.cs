using System;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public sealed class OrderDeliveryMovement
{
    private OrderDeliveryMovement() { }

    internal OrderDeliveryMovement(Order order, OrderDeliveryStep deliveryStep, OrderDeliveryStep replacementStep,
        Shipment oldShipment, DeliveryTrip oldDeliveryTrip, Shipment newShipment, DeliveryTrip newDeliveryTrip,
        DeliveryMethod previousMethod, int? previousPartyId, string previousPartyName,
        DeliveryMethod? nextMethod, int? nextPartyId, string nextPartyName, int? originalStepRank,
        OrderOperation operation, string reason, int version,
        OrderHistorySource source = OrderHistorySource.DeliveryRouteReassigned)
    {
        Order = order;
        OrderId = order.Id;
        DeliveryStep = deliveryStep;
        DeliveryStepId = deliveryStep?.Id > 0 ? deliveryStep.Id : null;
        ReplacementStep = replacementStep;
        OldShipment = oldShipment;
        OldShipmentId = oldShipment?.Id > 0 ? oldShipment.Id : null;
        OldDeliveryTrip = oldDeliveryTrip;
        OldDeliveryTripId = oldDeliveryTrip?.Id > 0 ? oldDeliveryTrip.Id : null;
        NewShipment = newShipment;
        NewDeliveryTrip = newDeliveryTrip;
        PreviousMethod = previousMethod;
        PreviousPartyId = previousPartyId;
        PreviousPartyName = previousPartyName;
        NextMethod = nextMethod;
        NextPartyId = nextPartyId;
        NextPartyName = nextPartyName;
        OriginalStepRank = originalStepRank;
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

    public int? DeliveryStepId { get; private set; }
    public OrderDeliveryStep DeliveryStep { get; private set; }

    public int? ReplacementStepId { get; private set; }
    public OrderDeliveryStep ReplacementStep { get; private set; }

    public int? OldShipmentId { get; private set; }
    public Shipment OldShipment { get; private set; }

    public int? OldDeliveryTripId { get; private set; }
    public DeliveryTrip OldDeliveryTrip { get; private set; }

    public int? NewShipmentId { get; private set; }
    public Shipment NewShipment { get; private set; }

    public int? NewDeliveryTripId { get; private set; }
    public DeliveryTrip NewDeliveryTrip { get; private set; }

    public DeliveryMethod PreviousMethod { get; private set; }
    public int? PreviousPartyId { get; private set; }
    public string PreviousPartyName { get; private set; }

    public DeliveryMethod? NextMethod { get; private set; }
    public int? NextPartyId { get; private set; }
    public string NextPartyName { get; private set; }

    public int? OriginalStepRank { get; private set; }
    public string Reason { get; private set; }

    public int ChangedByUserId { get; private set; }
    public string ChangedByUserName { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    public OrderHistorySource Source { get; private set; }
    public int OrderVersion { get; private set; }
}
