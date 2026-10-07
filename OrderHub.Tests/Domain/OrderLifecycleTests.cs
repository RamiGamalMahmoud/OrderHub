using System;
using System.Linq;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Domain;

public class OrderLifecycleTests
{
    private static OrderOperation Actor(Order order) => new(7, "Operator", order.CreatedAt);

    private static Order Create(DeliveryMethod method = DeliveryMethod.DeliveryChain)
    {
        var order = new Order(1, "TEST");
        order.UpdateHeader(1, OrderSource.Online, method, method == DeliveryMethod.DeliveryMan ? 2 : null,
            method == DeliveryMethod.ShippingCompany ? 3 : null, null);
        if (method == DeliveryMethod.DeliveryChain)
            for (var index = 1; index <= 2; index++)
            {
                var step = new OrderDeliveryStep { StepOrder = index * 10, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = 2 };
                typeof(OrderDeliveryStep).GetProperty(nameof(OrderDeliveryStep.Id)).SetValue(step, index);
                order.AddDeliveryStep(step);
            }
        Assert.True(order.InitializeHistory(Actor(order)).IsSuccess);
        return order;
    }

    [Fact]
    public void Creation_records_the_exact_creation_time_and_actor()
    {
        var order = Create();
        var initial = Assert.Single(order.StatusHistory);
        Assert.Equal(OrderStatus.Processing, order.OrderStatus);
        Assert.Null(initial.PreviousStatus);
        Assert.Equal(order.CreatedAt, initial.ChangedAtUtc);
        Assert.Equal(7, initial.ChangedByUserId);
        Assert.Equal(OrderHistorySource.Creation, initial.Source);
        Assert.False(order.InitializeHistory(Actor(order)).IsSuccess);
    }

    [Theory]
    [InlineData(DeliveryMethod.Pickup, OrderStatus.ReadyForPickup)]
    [InlineData(DeliveryMethod.DeliveryMan, OrderStatus.OutForDelivery)]
    [InlineData(DeliveryMethod.ShippingCompany, OrderStatus.Shipped)]
    [InlineData(DeliveryMethod.DeliveryChain, OrderStatus.OutForDelivery)]
    public void Initial_transitions_follow_the_method_and_lock_all_data(DeliveryMethod method, OrderStatus next)
    {
        foreach (var target in Enum.GetValues<OrderStatus>())
        {
            var candidate = Create(method);
            var result = candidate.TransitionTo(target, Actor(candidate), "Reason");
            Assert.Equal(target == next || target is OrderStatus.Processing or OrderStatus.Cancelled, result.IsSuccess);
        }
        var order = Create(method);
        Assert.True(order.TransitionTo(next, Actor(order)).IsSuccess);
        Assert.False(order.CanEdit);
        Assert.False(order.ChangePaymentMethod(9).IsSuccess);
        Assert.Throws<InvalidOperationException>(() => order.UpdateHeader(2, OrderSource.Online, method, null, null, null));
        Assert.False(order.TouchForEdit().IsSuccess);
    }

    [Theory]
    [InlineData(DeliveryMethod.DeliveryMan, OrderStatus.OutForDelivery)]
    [InlineData(DeliveryMethod.ShippingCompany, OrderStatus.Shipped)]
    public void Failures_and_retries_are_preserved_without_a_retry_status(DeliveryMethod method, OrderStatus active)
    {
        var order = Create(method);
        var actor = Actor(order);
        Assert.True(order.TransitionTo(active, actor).IsSuccess);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            Assert.True(order.TransitionTo(OrderStatus.DeliveryFailed, actor, "Absent").IsSuccess);
            Assert.True(order.TransitionTo(active, actor).IsSuccess);
        }
        Assert.Equal(2, order.StatusHistory.Count(history => history.Source == OrderHistorySource.Retry));
        Assert.Equal(2, order.StatusHistory.Count(history => history.NewStatus == OrderStatus.DeliveryFailed));
        Assert.True(order.TransitionTo(OrderStatus.Delivered, actor).IsSuccess);
        Assert.False(order.TransitionTo(OrderStatus.Cancelled, actor, "Cancel").IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Required_reasons_reject_empty_values_without_changes(string reason)
    {
        var order = Create();
        var version = order.Version;
        Assert.False(order.TransitionTo(OrderStatus.Cancelled, Actor(order), reason).IsSuccess);
        Assert.False(order.Archive(Actor(order), reason).IsSuccess);
        Assert.Equal(version, order.Version);
        Assert.Single(order.StatusHistory);
        Assert.False(order.IsArchived);
    }

    [Fact]
    public void Reason_limit_is_200_and_user_and_time_are_trusted_inputs()
    {
        Assert.True(Order.ValidateReason(new string('x', 200), true).IsSuccess);
        Assert.False(Order.ValidateReason(new string('x', 201), true).IsSuccess);
        var order = Create();
        Assert.False(order.TransitionTo(OrderStatus.Cancelled, new(0, "Operator", order.CreatedAt), "Cancel").IsSuccess);
        Assert.False(order.TransitionTo(OrderStatus.Cancelled, new(7, "Operator", DateTime.Now), "Cancel").IsSuccess);
        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void Sequence_failure_retry_and_last_completion_share_one_operation()
    {
        var order = Create();
        var actor = Actor(order);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, actor).IsSuccess);
        Assert.False(order.TransitionStep(2, DeliveryStatus.InProgress, actor).IsSuccess);
        Assert.False(order.TransitionStep(1, DeliveryStatus.Completed, actor).IsSuccess);
        Assert.True(order.TransitionStep(1, DeliveryStatus.InProgress, actor).IsSuccess);
        Assert.False(order.TransitionStep(1, DeliveryStatus.Failed, actor, " ").IsSuccess);
        Assert.True(order.TransitionStep(1, DeliveryStatus.Failed, actor, "Absent").IsSuccess);
        Assert.Equal(OrderStatus.OutForDelivery, order.OrderStatus);
        Assert.False(order.TransitionTo(OrderStatus.DeliveryFailed, actor, "Fail").IsSuccess);
        Assert.False(order.TransitionStep(2, DeliveryStatus.InProgress, actor).IsSuccess);
        Assert.True(order.TransitionStep(1, DeliveryStatus.InProgress, actor).IsSuccess);
        Assert.True(order.TransitionStep(1, DeliveryStatus.Completed, actor).IsSuccess);
        Assert.Equal(DeliveryStatus.Pending, order.DeliverySteps.Last().Status);
        Assert.True(order.TransitionStep(2, DeliveryStatus.InProgress, actor).IsSuccess);
        Assert.True(order.TransitionStep(2, DeliveryStatus.Completed, actor).IsSuccess);
        var completed = order.StepHistory.Last();
        var delivered = order.StatusHistory.Last();
        Assert.Equal(OrderStatus.Delivered, order.OrderStatus);
        Assert.Equal(completed.OrderVersion, delivered.OrderVersion);
        Assert.Equal(completed.ChangedAtUtc, delivered.ChangedAtUtc);
        Assert.Equal(completed.ChangedByUserId, delivered.ChangedByUserId);
        Assert.False(order.TransitionStep(1, DeliveryStatus.Failed, actor, "Fail").IsSuccess);
    }

    [Fact]
    public void Cancellation_preserves_completed_facts_and_cancels_only_unfinished_steps()
    {
        var order = Create();
        var actor = Actor(order);
        order.TransitionTo(OrderStatus.OutForDelivery, actor);
        order.TransitionStep(1, DeliveryStatus.InProgress, actor);
        order.TransitionStep(1, DeliveryStatus.Completed, actor);
        var completed = order.StepHistory.Last();
        Assert.True(order.TransitionTo(OrderStatus.Cancelled, actor, "Customer cancelled").IsSuccess);
        Assert.Equal(DeliveryStatus.Completed, order.DeliverySteps.First().Status);
        Assert.Equal(DeliveryStatus.Cancelled, order.DeliverySteps.Last().Status);
        Assert.Same(completed, order.StepHistory.Single(history => history.NewStatus == DeliveryStatus.Completed));
        Assert.Equal("Customer cancelled", order.StepHistory.Last().Reason);
        Assert.False(order.TransitionTo(OrderStatus.Processing, actor).IsSuccess);
    }

    [Theory]
    [InlineData(DeliveryStatus.Pending)]
    [InlineData(DeliveryStatus.InProgress)]
    [InlineData(DeliveryStatus.Failed)]
    [InlineData(DeliveryStatus.Cancelled)]
    public void Direct_delivery_keeps_or_sets_incomplete_results_and_closes_execution(DeliveryStatus remaining)
    {
        var order = Create();
        var actor = Actor(order);
        order.TransitionTo(OrderStatus.OutForDelivery, actor);
        if (remaining == DeliveryStatus.InProgress)
            order.TransitionStep(1, remaining, actor);
        var historyCount = order.StepHistory.Count;
        Assert.True(order.DeliverDirectly([new(1, remaining, "Reason"), new(2, DeliveryStatus.Completed)], actor, "Direct confirmation").IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.OrderStatus);
        Assert.Equal(remaining, order.DeliverySteps.First().Status);
        Assert.Equal(historyCount + (remaining is DeliveryStatus.Pending or DeliveryStatus.InProgress ? 1 : 2), order.StepHistory.Count);
        Assert.Empty(order.AllowedStepStatuses(order.DeliverySteps.First()));
        Assert.False(order.TransitionStep(1, DeliveryStatus.InProgress, actor).IsSuccess);
    }

    [Fact]
    public void Direct_delivery_validates_all_outcomes_before_any_mutation()
    {
        var order = Create();
        var actor = Actor(order);
        order.TransitionTo(OrderStatus.OutForDelivery, actor);
        var version = order.Version;
        Assert.False(order.DeliverDirectly([new(1, DeliveryStatus.Completed), new(2, DeliveryStatus.Failed, " ")], actor, "Direct").IsSuccess);
        Assert.False(order.DeliverDirectly([new(1, DeliveryStatus.Completed), new(1, DeliveryStatus.Completed)], actor, "Direct").IsSuccess);
        Assert.False(order.DeliverDirectly([new(1, DeliveryStatus.Completed), new(99, DeliveryStatus.Completed)], actor, "Direct").IsSuccess);
        Assert.Equal(version, order.Version);
        Assert.Empty(order.StepHistory);
        Assert.All(order.DeliverySteps, step => Assert.Equal(DeliveryStatus.Pending, step.Status));
    }

    [Fact]
    public void Noop_adds_no_event_and_archive_has_no_status_event_or_restore()
    {
        var order = Create();
        Assert.True(order.TransitionTo(OrderStatus.Processing, Actor(order)).IsSuccess);
        Assert.Equal(1, order.Version);
        Assert.True(order.Archive(Actor(order), "Duplicate order").IsSuccess);
        Assert.Equal(OrderStatus.Processing, order.OrderStatus);
        Assert.Single(order.StatusHistory);
        Assert.False(order.CanEdit);
        Assert.False(order.Archive(Actor(order), "Again").IsSuccess);
        Assert.False(order.TransitionTo(OrderStatus.Cancelled, Actor(order), "Cancel").IsSuccess);
        Assert.False(order.CheckVersion(1).IsSuccess);
    }

    [Fact]
    public void Empty_chain_cannot_start_and_active_order_must_cancel_before_archive()
    {
        var order = Create();
        order.ClearDeliverySteps();
        Assert.False(order.TransitionTo(OrderStatus.OutForDelivery, Actor(order)).IsSuccess);
        var active = Create();
        active.TransitionTo(OrderStatus.OutForDelivery, Actor(active));
        Assert.False(active.Archive(Actor(active), "Archive").IsSuccess);
        Assert.True(active.TransitionTo(OrderStatus.Cancelled, Actor(active), "Cancel").IsSuccess);
        Assert.True(active.Archive(Actor(active), "Archive").IsSuccess);
    }

    [Fact]
    public void Owned_steps_items_and_property_values_cannot_bypass_order_lock()
    {
        var order = Create();
        var item = order.AddOrderItem(1, "Saved", 10, 2, 0, null, null, [new(1, "Value")]).Value;
        Assert.False(item.DecreaseQuantity(2).IsSuccess);
        order.TransitionTo(OrderStatus.OutForDelivery, Actor(order));
        Assert.Throws<InvalidOperationException>(() => order.DeliverySteps.First().DeliverymanId = 99);
        Assert.Throws<InvalidOperationException>(() => order.DeliverySteps.First().StepOrder = 99);
        Assert.False(item.UpdateQuantity(3).IsSuccess);
        Assert.False(item.ChangeUnitPrice(20).IsSuccess);
        Assert.Throws<InvalidOperationException>(() => item.Properties.Single().Value = "Changed");
        Assert.Equal("Value", item.Properties.Single().Value);
    }
}
