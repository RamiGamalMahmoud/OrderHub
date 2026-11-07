using System.Reflection;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests;

public sealed class OrderReassignmentTests
{
    private static readonly DateTime AtUtc = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Chain_step_reassignment_replaces_the_step_at_the_same_rank_and_records_one_movement()
    {
        var (order, steps, operation) = NewChain((DeliveryMethod.DeliveryMan, 7), (DeliveryMethod.DeliveryMan, 9));
        var step = steps[0];
        Assert.True(order.TransitionStep(step.Id, DeliveryStatus.InProgress, operation).IsSuccess);
        Persist(step.DeliveryTrips.Single(), 51);
        var version = order.Version;

        var result = order.ReassignDeliveryStep(step.Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "New Deliveryman"), "Old Deliveryman", operation, "Capacity change");

        Assert.True(result.IsSuccess);
        Tombstone(step);
        Assert.Equal(version + 1, order.Version);
        Assert.Same(step, result.Value.CancelledStep);
        var replacement = result.Value.ReplacementStep;
        Assert.NotNull(replacement);
        Assert.Equal(1, replacement.StepOrder);
        Assert.Equal(DeliveryStatus.Pending, replacement.Status);
        Assert.Equal(8, replacement.DeliverymanId);
        Assert.Empty(replacement.DeliveryTrips);
        Assert.Equal(DeliveryStatus.Cancelled, step.Status);
        Assert.Equal(WorkRecordStatus.Cancelled, step.DeliveryTrips.Single().Status);

        var movement = Assert.Single(order.DeliveryMovements);
        Assert.Equal(DeliveryMethod.DeliveryChain, order.DeliveryMethod);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.PreviousMethod);
        Assert.Equal(7, movement.PreviousPartyId);
        Assert.Equal("Old Deliveryman", movement.PreviousPartyName);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.NextMethod);
        Assert.Equal(8, movement.NextPartyId);
        Assert.Equal("New Deliveryman", movement.NextPartyName);
        Assert.Equal(1, movement.OriginalStepRank);
        Assert.Equal(step.Id, movement.DeliveryStepId);
        Assert.Same(replacement, movement.ReplacementStep);
        Assert.Equal(51, movement.OldDeliveryTripId);
        Assert.Null(movement.NewDeliveryTrip);
        Assert.Equal(order.Version, movement.OrderVersion);
        Assert.Equal(OrderStatus.OutForDelivery, order.OrderStatus);
        Assert.Single(order.StatusHistory);
        Assert.Null(order.DeliveryReadinessError());
        Assert.Equal(2, order.ActiveSteps.Count);
    }

    [Fact]
    public void Superseded_step_is_not_reassignable_and_never_transitions()
    {
        var (order, steps, operation) = NewChain((DeliveryMethod.DeliveryMan, 7), (DeliveryMethod.DeliveryMan, 9));
        Assert.True(order.TransitionStep(steps[0].Id, DeliveryStatus.InProgress, operation).IsSuccess);
        Assert.True(order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Other"), "Old Deliveryman", operation, "Move").IsSuccess);
        Tombstone(steps[0]);

        var again = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(10, "Third"), "Old Deliveryman", operation, "Move again");

        Assert.False(again.IsSuccess);
        Assert.Contains("ملغاة", again.ErrorMessage);
        Assert.Empty(order.AllowedStepStatuses(steps[0]));
        var transition = order.TransitionStep(steps[0].Id, DeliveryStatus.InProgress, operation);
        Assert.False(transition.IsSuccess);
        Assert.Equal("الخطوة ملغاة أو مستبدلة.", transition.ErrorMessage);
        Assert.Single(order.DeliveryMovements);
    }

    [Fact]
    public void Sequencing_skips_the_tombstone_and_uses_the_immediate_active_predecessor()
    {
        var (order, steps, operation) = NewChain((DeliveryMethod.DeliveryMan, 7), (DeliveryMethod.DeliveryMan, 9));
        Assert.True(order.TransitionStep(steps[0].Id, DeliveryStatus.InProgress, operation).IsSuccess);
        var replacement = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Other"), "Old Deliveryman", operation, "Move").Value.ReplacementStep;
        Tombstone(steps[0]);

        Assert.Empty(order.AllowedStepStatuses(steps[1]));
        Assert.True(order.TransitionStep(replacement.Id, DeliveryStatus.InProgress, operation).IsSuccess);
        Assert.Empty(order.AllowedStepStatuses(steps[1]));
        Assert.True(order.TransitionStep(replacement.Id, DeliveryStatus.Completed, operation).IsSuccess);
        Assert.Equal(new[] { DeliveryStatus.InProgress }, order.AllowedStepStatuses(steps[1]));
        Assert.True(order.TransitionStep(steps[1].Id, DeliveryStatus.InProgress, operation).IsSuccess);
        Assert.True(order.TransitionStep(steps[1].Id, DeliveryStatus.Completed, operation).IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.OrderStatus);
    }

    [Fact]
    public void Readiness_requires_at_least_one_active_step()
    {
        var order = new Order(10, "ORD-TOMBSTONE");
        order.UpdateHeader(10, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
        order.AddDeliveryStep(new OrderDeliveryStep
        {
            StepOrder = -4,
            DeliveryMethod = DeliveryMethod.DeliveryMan,
            DeliverymanId = 7
        });

        Assert.Empty(order.ActiveSteps);
        Assert.Equal("يجب إضافة خطوة توصيل واحدة على الأقل.", order.DeliveryReadinessError());
    }

    [Fact]
    public void Chain_step_reassignment_rejects_unstarted_same_party_and_invalid_input()
    {
        var (order, steps, operation) = NewChain((DeliveryMethod.DeliveryMan, 7), (DeliveryMethod.DeliveryMan, 9));
        Assert.True(order.TransitionStep(steps[0].Id, DeliveryStatus.InProgress, operation).IsSuccess);
        var version = order.Version;

        var sameParty = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(7, "Same"), "Old Deliveryman", operation, "No-op");
        Assert.False(sameParty.IsSuccess);
        Assert.Contains("مختلفة", sameParty.ErrorMessage);

        var blankReason = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Other"), "Old Deliveryman", operation, "   ");
        Assert.False(blankReason.IsSuccess);

        var pickupWithParty = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.Pickup,
            new DeliveryParty(8, "Other"), "Old Deliveryman", operation, "Collect");
        Assert.False(pickupWithParty.IsSuccess);
        Assert.Contains("استلام", pickupWithParty.ErrorMessage);

        var pendingStep = order.ReassignDeliveryStep(steps[1].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Other"), "Other deliveryman", operation, "Move");
        Assert.False(pendingStep.IsSuccess);

        Assert.Equal(version, order.Version);
        Assert.Empty(order.DeliveryMovements);
    }

    [Fact]
    public void Direct_reassignment_switches_route_and_closes_the_pending_record()
    {
        var (order, operation) = NewDirect(DeliveryMethod.DeliveryMan, 7);
        var trip = order.DeliveryTrips.Single();
        Persist(trip, 61);
        var version = order.Version;

        var result = order.ReassignDirectDelivery(DeliveryMethod.ShippingCompany,
            new DeliveryParty(20, "Carrier"), "Deliveryman 7", operation, "Switch to carrier");

        Assert.True(result.IsSuccess);
        Assert.Equal(version + 1, order.Version);
        Assert.Equal(DeliveryMethod.ShippingCompany, order.DeliveryMethod);
        Assert.Null(order.DeliverymanId);
        Assert.Equal(20, order.ShippingCarrierId);
        Assert.Equal(OrderStatus.Shipped, order.OrderStatus);
        Assert.Equal(WorkRecordStatus.Cancelled, trip.Status);
        var shipment = Assert.Single(order.Shipments);
        Assert.Equal(WorkRecordStatus.Pending, shipment.Status);
        Assert.Single(order.StatusHistory);

        var movement = Assert.Single(order.DeliveryMovements);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.PreviousMethod);
        Assert.Equal(7, movement.PreviousPartyId);
        Assert.Equal(DeliveryMethod.ShippingCompany, movement.NextMethod);
        Assert.Equal(20, movement.NextPartyId);
        Assert.Equal(61, movement.OldDeliveryTripId);
        Assert.Same(shipment, movement.NewShipment);
        Assert.Null(movement.OriginalStepRank);
        Assert.Equal(order.Version, movement.OrderVersion);
    }

    [Fact]
    public void Direct_reassignment_from_a_failed_attempt_leaves_the_failed_record_untouched()
    {
        var (order, operation) = NewDirect(DeliveryMethod.DeliveryMan, 7);
        var trip = order.DeliveryTrips.Single();
        Persist(trip, 62);
        Assert.True(order.TransitionTo(OrderStatus.DeliveryFailed, operation, "Customer unavailable").IsSuccess);

        var result = order.ReassignDirectDelivery(DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Other"), "Deliveryman 7", operation, "Retry elsewhere");

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkRecordStatus.Failed, trip.Status);
        Assert.Equal(OrderStatus.OutForDelivery, order.OrderStatus);
        Assert.Equal(2, order.DeliveryTrips.Count);
        Assert.Equal(WorkRecordStatus.Pending, order.DeliveryTrips.Last().Status);
        Assert.Equal(62, Assert.Single(order.DeliveryMovements).OldDeliveryTripId);
    }

    [Fact]
    public void Direct_reassignment_rejects_the_current_party_and_invalid_states()
    {
        var (order, operation) = NewDirect(DeliveryMethod.DeliveryMan, 7);
        var version = order.Version;

        var sameParty = order.ReassignDirectDelivery(DeliveryMethod.DeliveryMan,
            new DeliveryParty(7, "Same"), "Deliveryman 7", operation, "No-op");
        Assert.False(sameParty.IsSuccess);
        Assert.Contains("مختلفة", sameParty.ErrorMessage);

        var noParty = order.ReassignDirectDelivery(DeliveryMethod.ShippingCompany,
            null, "Deliveryman 7", operation, "Switch");
        Assert.False(noParty.IsSuccess);

        Assert.Equal(version, order.Version);
        Assert.Empty(order.DeliveryMovements);
        Assert.Single(order.DeliveryTrips);
    }

    [Fact]
    public void Pickup_from_chain_aborts_the_chain_and_confirms_through_one_path()
    {
        var (order, steps, operation) = NewChain((DeliveryMethod.DeliveryMan, 7), (DeliveryMethod.DeliveryMan, 9));
        Assert.True(order.TransitionStep(steps[0].Id, DeliveryStatus.InProgress, operation).IsSuccess);
        Persist(steps[0].DeliveryTrips.Single(), 71);

        var result = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.Pickup,
            null, "Deliveryman 7", operation, "Customer will collect");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ReplacementStep);
        Assert.Equal(DeliveryMethod.Pickup, order.DeliveryMethod);
        Assert.Null(order.DeliverymanId);
        Assert.Null(order.ShippingCarrierId);
        Assert.Equal(OrderStatus.ReadyForPickup, order.OrderStatus);
        Assert.Equal(DeliveryStatus.Cancelled, steps[0].Status);
        Assert.Equal(DeliveryStatus.Cancelled, steps[1].Status);
        Assert.Equal(WorkRecordStatus.Cancelled, steps[0].DeliveryTrips.Single().Status);
        Assert.Single(order.StatusHistory);

        var movement = Assert.Single(order.DeliveryMovements);
        Assert.Equal(DeliveryMethod.Pickup, movement.NextMethod);
        Assert.Null(movement.NextPartyId);
        Assert.Null(movement.NextPartyName);
        Assert.Equal(1, movement.OriginalStepRank);
        Assert.Equal(71, movement.OldDeliveryTripId);

        var version = order.Version;
        Assert.True(order.TransitionTo(OrderStatus.Delivered, operation).IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.OrderStatus);
        Assert.Equal(version + 1, order.Version);
        Assert.Equal(OrderHistorySource.PickupConfirmed, order.StatusHistory.Last().Source);
    }

    [Fact]
    public void Pickup_from_direct_clears_both_parties_and_creates_no_record()
    {
        var (order, operation) = NewDirect(DeliveryMethod.DeliveryMan, 7);
        var trip = order.DeliveryTrips.Single();
        Persist(trip, 72);

        var result = order.ReassignDirectDelivery(DeliveryMethod.Pickup,
            null, "Deliveryman 7", operation, "Customer will collect");

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.ReadyForPickup, order.OrderStatus);
        Assert.Equal(DeliveryMethod.Pickup, order.DeliveryMethod);
        Assert.Null(order.DeliverymanId);
        Assert.Null(order.ShippingCarrierId);
        Assert.Equal(WorkRecordStatus.Cancelled, trip.Status);
        Assert.Empty(order.Shipments);
        Assert.Single(order.DeliveryTrips);

        var movement = Assert.Single(order.DeliveryMovements);
        Assert.Null(movement.NewShipment);
        Assert.Null(movement.NewDeliveryTrip);
        Assert.Equal(72, movement.OldDeliveryTripId);

        Assert.True(order.TransitionTo(OrderStatus.Delivered, operation).IsSuccess);
        Assert.Equal(OrderHistorySource.PickupConfirmed, order.StatusHistory.Last().Source);
    }

    [Fact]
    public void Repeated_reassignment_of_the_replacement_keeps_the_effective_rank()
    {
        var (order, steps, operation) = NewChain((DeliveryMethod.DeliveryMan, 7), (DeliveryMethod.DeliveryMan, 9));
        Assert.True(order.TransitionStep(steps[0].Id, DeliveryStatus.InProgress, operation).IsSuccess);
        var replacement = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Second"), "First", operation, "First move").Value.ReplacementStep;
        Tombstone(steps[0]);
        Persist(replacement, 3);
        Assert.True(order.TransitionStep(replacement.Id, DeliveryStatus.InProgress, operation).IsSuccess);

        var second = order.ReassignDeliveryStep(replacement.Id, DeliveryMethod.ShippingCompany,
            new DeliveryParty(20, "Third"), "Second", operation, "Second move");

        Assert.True(second.IsSuccess);
        Tombstone(replacement);
        Assert.Equal(1, second.Value.ReplacementStep.StepOrder);
        Assert.Equal(20, second.Value.ReplacementStep.ShippingCarrierId);
        Assert.Equal(DeliveryMethod.DeliveryChain, order.DeliveryMethod);
        Assert.Equal(2, order.DeliveryMovements.Count);
        Assert.All(order.DeliveryMovements, movement => Assert.Equal(1, movement.OriginalStepRank));
        Assert.Equal(2, order.ActiveSteps.Count);
    }

    [Fact]
    public void Deliver_directly_ignores_superseded_steps()
    {
        var (order, steps, operation) = NewChain((DeliveryMethod.DeliveryMan, 7), (DeliveryMethod.DeliveryMan, 9));
        Assert.True(order.TransitionStep(steps[0].Id, DeliveryStatus.InProgress, operation).IsSuccess);
        var replacement = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Second"), "First", operation, "Move").Value.ReplacementStep;
        Tombstone(steps[0]);

        var outcomes = new[]
        {
            new DeliveryStepOutcome(replacement.Id, DeliveryStatus.Cancelled, "Abort"),
            new DeliveryStepOutcome(steps[1].Id, DeliveryStatus.Cancelled, "Abort")
        };

        Assert.True(order.CanDeliverDirectly);
        Assert.True(order.DeliverDirectly(outcomes, operation, "Close").IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.OrderStatus);
    }

    [Fact]
    public void Final_orders_reject_reassignment()
    {
        var (order, steps, operation) = NewChain((DeliveryMethod.DeliveryMan, 7));
        Assert.True(order.TransitionStep(steps[0].Id, DeliveryStatus.InProgress, operation).IsSuccess);
        Assert.True(order.TransitionTo(OrderStatus.Cancelled, operation, "Stop").IsSuccess);

        var result = order.ReassignDeliveryStep(steps[0].Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Other"), "First", operation, "Move");

        Assert.False(result.IsSuccess);
        Assert.Contains("مغلق", result.ErrorMessage);
        Assert.Empty(order.DeliveryMovements);
    }

    private static (Order Order, List<OrderDeliveryStep> Steps, OrderOperation Operation) NewChain(
        params (DeliveryMethod Method, int PartyId)[] handlers)
    {
        var order = new Order(10, "ORD-CHAIN-MOVE");
        order.UpdateHeader(10, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
        var steps = new List<OrderDeliveryStep>();
        var rank = 1;
        foreach (var (method, partyId) in handlers)
        {
            var step = new OrderDeliveryStep
            {
                StepOrder = rank++,
                DeliveryMethod = method,
                DeliverymanId = method == DeliveryMethod.DeliveryMan ? partyId : null,
                ShippingCarrierId = method == DeliveryMethod.ShippingCompany ? partyId : null
            };
            order.AddDeliveryStep(step);
            steps.Add(step);
        }
        var operation = new OrderOperation(1, "Test Operator", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, operation).IsSuccess);
        for (var index = 0; index < steps.Count; index++) Persist(steps[index], index + 1);
        return (order, steps, operation);
    }

    private static (Order Order, OrderOperation Operation) NewDirect(DeliveryMethod method, int partyId)
    {
        var order = new Order(10, "ORD-DIRECT-MOVE");
        order.UpdateHeader(10, OrderSource.Online, method,
            method == DeliveryMethod.DeliveryMan ? partyId : null,
            method == DeliveryMethod.ShippingCompany ? partyId : null, null);
        var operation = new OrderOperation(1, "Test Operator", AtUtc);
        var started = method == DeliveryMethod.ShippingCompany ? OrderStatus.Shipped : OrderStatus.OutForDelivery;
        Assert.True(order.TransitionTo(started, operation).IsSuccess);
        return (order, operation);
    }

    private static void Persist(object entity, int id) =>
        typeof(ModelBase).GetProperty(nameof(ModelBase.Id))!.SetValue(entity, id);

    private static void Tombstone(OrderDeliveryStep step) =>
        typeof(OrderDeliveryStep).GetField("_stepOrder", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(step, -step.Id);
}
