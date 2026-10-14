using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests;

public sealed class WorkRecordLifecycleTests
{
    private static readonly DateTime AtUtc = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Creation_starts_pending_and_records_initial_history()
    {
        var supply = NewSupply();

        Assert.Equal(WorkRecordStatus.Pending, supply.Status);
        Assert.Empty(supply.StatusHistory);
        ((IWorkRecord)supply).AppendInitialStatusHistory();
        Assert.Collection(supply.StatusHistory, entry =>
        {
            Assert.Equal(WorkRecordType.SupplierSupply, entry.WorkRecordType);
            Assert.Equal(WorkRecordStatus.Pending, entry.Status);
            Assert.Null(entry.Reason);
            Assert.Equal(DateTimeKind.Utc, entry.CreatedAtUtc.Kind);
        });
    }

    [Fact]
    public void Failure_requires_reason_updates_status_and_appends_history()
    {
        var supply = NewSupply();
        ((IWorkRecord)supply).AppendInitialStatusHistory();

        Assert.Throws<InvalidOperationException>(() => supply.TransitionTo(WorkRecordStatus.Failed, "  ", AtUtc));
        supply.TransitionTo(WorkRecordStatus.Failed, "Supplier unavailable", AtUtc);

        Assert.Equal(WorkRecordStatus.Failed, supply.Status);
        Assert.Equal(new[] { WorkRecordStatus.Pending, WorkRecordStatus.Failed }, supply.StatusHistory.Select(x => x.Status));
        Assert.Equal("Supplier unavailable", supply.StatusHistory.Last().Reason);
    }

    [Fact]
    public void Retry_is_a_new_independent_record_and_preserves_failed_attempt()
    {
        var failed = NewSupply();
        ((IWorkRecord)failed).AppendInitialStatusHistory();
        failed.TransitionTo(WorkRecordStatus.Failed, "First attempt failed", AtUtc);
        var retry = NewSupply();
        ((IWorkRecord)retry).AppendInitialStatusHistory();

        Assert.Equal(WorkRecordStatus.Failed, failed.Status);
        Assert.Equal(2, failed.StatusHistory.Count);
        Assert.Equal(WorkRecordStatus.Pending, retry.Status);
        Assert.Single(retry.StatusHistory);
        Assert.NotSame(failed, retry);
    }

    [Fact]
    public void Supply_is_linked_to_its_order_and_supplier()
    {
        var order = new Order(10, "ORD-SUPPLY");
        var supply = new Supply(order, 25);

        Assert.Same(order, supply.Order);
        Assert.Equal(order.Id, supply.OrderId);
        Assert.Equal(25, supply.SupplierId);
    }

    [Fact]
    public void Previous_status_history_is_retained_after_later_transitions()
    {
        var supply = NewSupply();
        ((IWorkRecord)supply).AppendInitialStatusHistory();
        supply.TransitionTo(WorkRecordStatus.Failed, "First attempt failed", AtUtc);

        var historyAfterFailure = supply.StatusHistory.ToArray();
        var retry = NewSupply();
        ((IWorkRecord)retry).AppendInitialStatusHistory();
        retry.TransitionTo(WorkRecordStatus.Completed, atUtc: AtUtc);

        Assert.Equal(2, historyAfterFailure.Length);
        Assert.Equal(WorkRecordStatus.Failed, historyAfterFailure[1].Status);
        Assert.Equal(new[] { WorkRecordStatus.Pending, WorkRecordStatus.Completed }, retry.StatusHistory.Select(x => x.Status));
    }

    [Fact]
    public void Completion_appends_history_and_closes_the_attempt()
    {
        var supply = NewSupply();
        ((IWorkRecord)supply).AppendInitialStatusHistory();
        supply.TransitionTo(WorkRecordStatus.Completed, atUtc: AtUtc);

        Assert.Equal(WorkRecordStatus.Completed, supply.Status);
        Assert.Equal(new[] { WorkRecordStatus.Pending, WorkRecordStatus.Completed }, supply.StatusHistory.Select(x => x.Status));
        Assert.Throws<InvalidOperationException>(() => supply.TransitionTo(WorkRecordStatus.Failed, "late failure", AtUtc));
    }

    [Fact]
    public void Cancellation_requires_reason_and_closes_the_attempt()
    {
        var supply = NewSupply();
        ((IWorkRecord)supply).AppendInitialStatusHistory();

        Assert.Throws<InvalidOperationException>(() => supply.TransitionTo(WorkRecordStatus.Cancelled, null, AtUtc));
        supply.TransitionTo(WorkRecordStatus.Cancelled, "Order withdrawn", AtUtc);

        Assert.Equal(WorkRecordStatus.Cancelled, supply.Status);
        Assert.Equal("Order withdrawn", supply.StatusHistory.Last().Reason);
        Assert.Throws<InvalidOperationException>(() => supply.TransitionTo(WorkRecordStatus.Completed, atUtc: AtUtc));
    }

    [Fact]
    public void Duplicate_and_invalid_transitions_do_not_create_history()
    {
        var supply = NewSupply();
        ((IWorkRecord)supply).AppendInitialStatusHistory();
        supply.TransitionTo(WorkRecordStatus.Pending, atUtc: AtUtc);
        Assert.Throws<InvalidOperationException>(() => supply.TransitionTo((WorkRecordStatus)42, atUtc: AtUtc));
        Assert.Single(supply.StatusHistory);
    }

    [Fact]
    public void Delivery_step_retry_creates_separate_attempts_associated_with_the_step()
    {
        var (order, step, operation) = NewDeliveryChain();
        Assert.True(order.TransitionStep(step.Id, DeliveryStatus.InProgress, operation).IsSuccess);
        Persist(step.DeliveryTrips.Single(), 101);
        Assert.True(order.TransitionStep(step.Id, DeliveryStatus.Failed, operation, "First trip failed").IsSuccess);
        Assert.True(order.TransitionStep(step.Id, DeliveryStatus.InProgress, operation).IsSuccess);

        Assert.Equal(2, step.DeliveryTrips.Count);
        Assert.All(step.DeliveryTrips, trip => Assert.Same(step, trip.DeliveryStep));
        Assert.Equal(WorkRecordStatus.Failed, step.DeliveryTrips.First().Status);
        Assert.Equal(WorkRecordStatus.Pending, step.DeliveryTrips.Last().Status);
    }

    [Fact]
    public void Delivery_step_completion_closes_current_trip()
    {
        var (order, step, operation) = NewDeliveryChain();
        Assert.True(order.TransitionStep(step.Id, DeliveryStatus.InProgress, operation).IsSuccess);
        Persist(step.DeliveryTrips.Single(), 102);
        Assert.True(order.TransitionStep(step.Id, DeliveryStatus.Completed, operation).IsSuccess);

        var trip = Assert.Single(step.DeliveryTrips);
        Assert.Equal(WorkRecordStatus.Completed, trip.Status);
        Assert.Equal(WorkRecordStatus.Pending, trip.StatusHistory.First().Status);
        Assert.Equal(WorkRecordStatus.Completed, trip.StatusHistory.Last().Status);
    }

    private static Supply NewSupply() => new(new Order(10, "ORD-TEST"), 5);

    private static void Persist(IWorkRecord record, int id)
    {
        record.AppendInitialStatusHistory();
        typeof(ModelBase).GetProperty(nameof(ModelBase.Id))!.SetValue(record, id);
    }

    private static (Order Order, OrderDeliveryStep Step, OrderOperation Operation) NewDeliveryChain()
    {
        var order = new Order(10, "ORD-CHAIN");
        order.UpdateHeader(10, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
        var step = new OrderDeliveryStep
        {
            StepOrder = 1,
            DeliveryMethod = DeliveryMethod.DeliveryMan,
            DeliverymanId = 7
        };
        order.AddDeliveryStep(step);
        var operation = new OrderOperation(1, "Test Operator", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, operation).IsSuccess);
        return (order, step, operation);
    }
}

