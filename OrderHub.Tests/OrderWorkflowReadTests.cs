using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Orders;
using OrderHub.Infrastructure.Reopsitories;

namespace OrderHub.Tests;

public sealed class OrderWorkflowReadTests
{
    private static readonly DateTime AtUtc = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Chain_reassignment_exposes_effective_ranks_counters_and_merged_history()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (client, seedUser, operatorUser, firstParty, secondParty, nextParty) =
            await SeedPartiesAsync(context);
        var order = NewChainOrder(client, firstParty, secondParty);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var first = order.DeliverySteps.OrderBy(step => step.StepOrder).First();
        var second = order.DeliverySteps.OrderBy(step => step.StepOrder).Last();
        var seed = new OrderOperation(seedUser.Id, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        Assert.True(order.TransitionStep(first.Id, DeliveryStatus.InProgress, seed).IsSuccess);
        ((IWorkRecord)first.DeliveryTrips.Single()).AssignWorkRecordNumber(firstParty.Id, 1, 2026, 11);
        await context.SaveChangesAsync();
        var oldStepId = first.Id;

        var operation = new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(5));
        Assert.True(order.ReassignDeliveryStep(oldStepId, DeliveryMethod.DeliveryMan,
            new DeliveryParty(nextParty.Id, "Receiving"), "Original", operation, "Moved").IsSuccess);
        var repository = new OrderRepository(context);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await repository.TombstoneDeliveryStepAsync(first, CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var state = await Reader().GetWorkflowAsync(order.Id, CancellationToken.None);

        Assert.Equal(2, state.ActiveStepCount);
        Assert.Equal(0, state.CompletedActiveStepCount);
        Assert.False(state.CanReassignDirectly);
        Assert.Equal(operation.AtUtc, state.LastChangedAtUtc);
        Assert.Equal("Moved", state.LatestReason);

        Assert.Equal(3, state.Steps.Count);
        var superseded = state.Steps[0];
        Assert.Equal(oldStepId, superseded.Id);
        Assert.True(superseded.IsSuperseded);
        Assert.Equal(1, superseded.EffectiveRank);
        Assert.Equal(-oldStepId, superseded.StepOrder);
        Assert.StartsWith("الخطوة ملغاة", superseded.BlockingReason);
        Assert.False(superseded.CanReassign);
        var replacement = state.Steps[1];
        Assert.False(replacement.IsSuperseded);
        Assert.Equal(1, replacement.EffectiveRank);
        Assert.Equal(DeliveryStatus.Pending, replacement.Status);
        Assert.True(replacement.CanReassign);
        Assert.Equal(2, state.Steps[2].EffectiveRank);
        Assert.Equal(second.Id, state.Steps[2].Id);

        var history = await Reader().GetHistoryAsync(order.Id, null, 1, 50, CancellationToken.None);
        Assert.Equal(2, history.TotalCount);
        Assert.Equal(history.Items.Count(), history.Items.Select(entry => entry.Key).Distinct().Count());
        var movement = history.Items.First();
        Assert.StartsWith("movement:", movement.Key);
        Assert.Equal(OrderHistorySource.DeliveryRouteReassigned, movement.Source);
        Assert.Equal("إعادة توجيه", movement.DisplayTag);
        Assert.Equal("مندوب توصيل — Original ← مندوب توصيل — Receiving", movement.DisplayDetail);
        Assert.Equal("Moved", movement.Reason);
        Assert.Equal("Operator", movement.UserName);
        Assert.All(history.Items.Skip(1), entry => Assert.StartsWith("order:", entry.Key));
    }

    [Fact]
    public async Task Pending_chain_steps_offer_reassignment_and_cancellation_before_execution()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (client, seedUser, _, firstParty, secondParty, _) = await SeedPartiesAsync(context);
        var order = NewChainOrder(client, firstParty, secondParty);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery,
            new OrderOperation(seedUser.Id, "Seed", AtUtc)).IsSuccess);
        await context.SaveChangesAsync();

        var state = await Reader().GetWorkflowAsync(order.Id, CancellationToken.None);

        Assert.Equal(2, state.Steps.Count);
        Assert.All(state.Steps, step =>
        {
            Assert.Equal(DeliveryStatus.Pending, step.Status);
            Assert.True(step.CanReassign);
            Assert.True(step.CanCancel);
        });
    }

    [Fact]
    public async Task Cancelling_a_step_marks_it_superseded_and_shows_a_cancellation_row()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (client, seedUser, operatorUser, firstParty, secondParty, _) =
            await SeedPartiesAsync(context);
        var order = NewChainOrder(client, firstParty, secondParty);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var first = order.DeliverySteps.OrderBy(step => step.StepOrder).First();
        var second = order.DeliverySteps.OrderBy(step => step.StepOrder).Last();
        var seed = new OrderOperation(seedUser.Id, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        Assert.True(order.TransitionStep(first.Id, DeliveryStatus.InProgress, seed).IsSuccess);
        ((IWorkRecord)first.DeliveryTrips.Single()).AssignWorkRecordNumber(firstParty.Id, 1, 2026, 11);
        await context.SaveChangesAsync();

        var operation = new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(5));
        Assert.True(order.CancelDeliveryStep(second.Id, "Second", operation, "Input error").IsSuccess);
        var repository = new OrderRepository(context);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await repository.TombstoneDeliveryStepAsync(second, CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var state = await Reader().GetWorkflowAsync(order.Id, CancellationToken.None);

        Assert.Equal(1, state.ActiveStepCount);
        var cancelled = state.Steps.Single(step => step.Id == second.Id);
        Assert.True(cancelled.IsSuperseded);
        Assert.Equal(2, cancelled.EffectiveRank);
        Assert.Equal(DeliveryStatus.Cancelled, cancelled.Status);
        Assert.False(cancelled.CanReassign);
        Assert.False(cancelled.CanCancel);
        var running = state.Steps.Single(step => step.Id == first.Id);
        Assert.True(running.CanReassign);
        Assert.False(running.CanCancel);

        var history = await Reader().GetHistoryAsync(order.Id, null, 1, 50, CancellationToken.None);
        var movement = history.Items.Single(entry => entry.Source == OrderHistorySource.DeliveryStepCancelled);
        Assert.Equal("إلغاء خطوة", movement.DisplayTag);
        Assert.Equal("مندوب توصيل — Second ← أُلغيت دون بديل", movement.DisplayDetail);
        Assert.Equal("مندوب توصيل — Second", movement.PreviousStatus);
        Assert.Null(movement.NewStatus);
        Assert.Equal("Input error", movement.Reason);
        Assert.Equal("Operator", movement.UserName);
        Assert.Equal(history.Items.Count(), history.Items.Select(entry => entry.Key).Distinct().Count());
    }

    [Fact]
    public async Task Pickup_conversion_keeps_aborted_steps_and_confirmation_is_tagged_in_history()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (client, seedUser, operatorUser, firstParty, secondParty, _) =
            await SeedPartiesAsync(context);
        var order = NewChainOrder(client, firstParty, secondParty);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var first = order.DeliverySteps.OrderBy(step => step.StepOrder).First();
        var second = order.DeliverySteps.OrderBy(step => step.StepOrder).Last();
        var seed = new OrderOperation(seedUser.Id, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        Assert.True(order.TransitionStep(first.Id, DeliveryStatus.InProgress, seed).IsSuccess);
        ((IWorkRecord)first.DeliveryTrips.Single()).AssignWorkRecordNumber(firstParty.Id, 1, 2026, 11);
        await context.SaveChangesAsync();

        Assert.True(order.ReassignDeliveryStep(first.Id, DeliveryMethod.Pickup, null, "Original",
            new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(5)), "Branch pickup").IsSuccess);
        var repository = new OrderRepository(context);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await repository.TombstoneDeliveryStepAsync(first, CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var state = await Reader().GetWorkflowAsync(order.Id, CancellationToken.None);
        Assert.Equal(OrderStatus.ReadyForPickup, state.Status);
        Assert.Equal(DeliveryMethod.Pickup, state.Method);
        Assert.Equal("استلام من المقر", state.HandlerName);
        Assert.Equal(0, state.ActiveStepCount);
        Assert.Equal(0, state.CompletedActiveStepCount);
        Assert.False(state.CanReassignDirectly);
        Assert.Equal(2, state.Steps.Count);
        Assert.True(state.Steps[0].IsSuperseded);
        Assert.False(state.Steps[1].IsSuperseded);
        Assert.Equal(second.Id, state.Steps[1].Id);
        Assert.Equal(2, state.Steps[1].EffectiveRank);
        Assert.Equal(DeliveryStatus.Cancelled, state.Steps[1].Status);
        Assert.StartsWith("الخطوة ملغاة", state.Steps[1].BlockingReason);

        context.ChangeTracker.Clear();
        var reloaded = await repository.GetForLifecycleAsync(order.Id);
        Assert.True(reloaded.TransitionTo(OrderStatus.Delivered,
            new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(10))).IsSuccess);
        await context.SaveChangesAsync();

        var history = await Reader().GetHistoryAsync(order.Id, null, 1, 50, CancellationToken.None);
        var confirmation = history.Items.First();
        Assert.Equal(OrderHistorySource.PickupConfirmed, confirmation.Source);
        Assert.Equal("تأكيد استلام", confirmation.DisplayTag);
        Assert.Null(confirmation.DisplayDetail);
        Assert.Equal("جاهز للتسليم", confirmation.PreviousStatus);
        Assert.Equal("تم التسليم", confirmation.NewStatus);
        var movement = history.Items.Single(entry => entry.Source == OrderHistorySource.DeliveryRouteReassigned);
        Assert.Equal("مندوب توصيل — Original ← استلام من المقر", movement.DisplayDetail);
        Assert.Equal(history.Items.Count(), history.Items.Select(entry => entry.Key).Distinct().Count());
    }

    [Fact]
    public async Task Direct_route_switch_is_discoverable_and_offers_reassignment()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (client, seedUser, operatorUser, firstParty, _, _) = await SeedPartiesAsync(context);
        var carrier = new ShippingCarrier(
            "Carrier",
            25m,
            Phone.Create("0504444444", "+966").Value,
            Address.Create("King Fahd Road", firstParty.City).Value);
        context.ShippingCarriers.Add(carrier);
        await context.SaveChangesAsync();

        var order = new Order(client.Id, "ORD-DIRECT");
        order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.DeliveryMan, firstParty.Id, null, null);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery,
            new OrderOperation(seedUser.Id, "Seed", AtUtc)).IsSuccess);
        ((IWorkRecord)order.DeliveryTrips.Single()).AssignWorkRecordNumber(firstParty.Id, 1, 2026, 11);
        Assert.True(order.ReassignDirectDelivery(DeliveryMethod.ShippingCompany,
            new DeliveryParty(carrier.Id, "Carrier"), "Original",
            new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(5)), "Switch route").IsSuccess);
        ((IWorkRecord)order.Shipments.Single()).AssignWorkRecordNumber(carrier.Id, 1, 2026, 11);
        await context.SaveChangesAsync();

        var savedMovement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Equal(DeliveryMethod.DeliveryMan, savedMovement.PreviousMethod);
        Assert.Equal(DeliveryMethod.ShippingCompany, savedMovement.NextMethod);

        var state = await Reader().GetWorkflowAsync(order.Id, CancellationToken.None);
        Assert.Equal(OrderStatus.Shipped, state.Status);
        Assert.Equal(DeliveryMethod.ShippingCompany, state.Method);
        Assert.True(state.CanReassignDirectly);
        Assert.Empty(state.Steps);
        Assert.Equal(0, state.ActiveStepCount);
        Assert.Equal(0, state.CompletedActiveStepCount);

        var history = await Reader().GetHistoryAsync(order.Id, null, 1, 10, CancellationToken.None);
        var movement = history.Items.First();
        Assert.StartsWith("movement:", movement.Key);
        Assert.Equal("مندوب توصيل — Original ← شركة شحن — Carrier", movement.DisplayDetail);
        Assert.Equal("مندوب توصيل — Original", movement.PreviousStatus);
        Assert.Equal("شركة شحن — Carrier", movement.NewStatus);
        Assert.Contains(history.Items, entry => entry.Source == OrderHistorySource.ManualAction && entry.Key.StartsWith("order:"));
    }

    private static OrderReader Reader() =>
        new(new AppDbContextFactory(PostgresTestDatabase.Options));

    private static async Task<(Client Client, User SeedUser, User OperatorUser, Deliveryman First,
        Deliveryman Second, Deliveryman Next)> SeedPartiesAsync(AppDbContext context)
    {
        var city = new City("Riyadh");
        var client = new Client("Client", "Street", city, "0501111111", "+966");
        var seedUser = User.Create("seed", "hash", "Seed");
        var operatorUser = User.Create("operator", "hash", "Operator");
        var first = new Deliveryman("First", city, "0500000001");
        var second = new Deliveryman("Second", city, "0500000002");
        var next = new Deliveryman("Third", city, "0500000003");
        context.Cities.Add(city);
        context.Clients.Add(client);
        context.Users.AddRange(seedUser, operatorUser);
        context.Deliverymen.AddRange(first, second, next);
        await context.SaveChangesAsync();
        return (client, seedUser, operatorUser, first, second, next);
    }

    private static Order NewChainOrder(Client client, Deliveryman first, Deliveryman second)
    {
        var order = new Order(client.Id, "ORD-READ");
        order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
        order.AddDeliveryStep(new OrderDeliveryStep
        {
            StepOrder = 1,
            DeliveryMethod = DeliveryMethod.DeliveryMan,
            DeliverymanId = first.Id
        });
        order.AddDeliveryStep(new OrderDeliveryStep
        {
            StepOrder = 2,
            DeliveryMethod = DeliveryMethod.DeliveryMan,
            DeliverymanId = second.Id
        });
        return order;
    }
}
