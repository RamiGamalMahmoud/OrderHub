using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Reopsitories;

namespace OrderHub.Tests;

public sealed class OrderReassignmentPersistenceTests
{
    private static readonly DateTime AtUtc = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Chain_reassignment_tombstones_the_old_step_and_persists_the_movement()
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
        var trip = first.DeliveryTrips.Single();
        ((IWorkRecord)trip).AssignWorkRecordNumber(firstParty.Id, 1, 2026, 11);
        await context.SaveChangesAsync();
        var oldStepId = first.Id;
        var secondStepId = second.Id;

        var operation = new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(5));
        var reassignment = order.ReassignDeliveryStep(oldStepId, DeliveryMethod.DeliveryMan,
            new DeliveryParty(nextParty.Id, "Receiving"), "Original", operation, "Moved");
        Assert.True(reassignment.IsSuccess);
        var repository = new OrderRepository(context);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await repository.TombstoneDeliveryStepAsync(first, CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        context.ChangeTracker.Clear();
        var reloaded = await repository.GetForLifecycleAsync(order.Id);
        Assert.Equal(OrderStatus.OutForDelivery, reloaded.OrderStatus);
        var oldStep = reloaded.DeliverySteps.Single(step => step.Id == oldStepId);
        Assert.Equal(-oldStepId, oldStep.StepOrder);
        Assert.Equal(DeliveryStatus.Cancelled, oldStep.Status);
        var replacement = reloaded.DeliverySteps.Single(step => step.Id != oldStepId && step.Id != secondStepId);
        Assert.Equal(1, replacement.StepOrder);
        Assert.Equal(nextParty.Id, replacement.DeliverymanId);
        Assert.Equal(DeliveryStatus.Pending, replacement.Status);
        Assert.Equal(2, reloaded.DeliverySteps.Single(step => step.Id == secondStepId).StepOrder);

        var savedTrip = await context.DeliveryTrips.AsNoTracking().SingleAsync(record => record.Id == trip.Id);
        Assert.Equal(WorkRecordStatus.Cancelled, savedTrip.Status);
        Assert.Equal(firstParty.Id, savedTrip.NumberingPartyId);
        Assert.Equal(1L, savedTrip.WorkRecordNumber);

        var movement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Equal(order.Id, movement.OrderId);
        Assert.Equal(oldStepId, movement.DeliveryStepId);
        Assert.Equal(replacement.Id, movement.ReplacementStepId);
        Assert.Equal(1, movement.OriginalStepRank);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.PreviousMethod);
        Assert.Equal(firstParty.Id, movement.PreviousPartyId);
        Assert.Equal("Original", movement.PreviousPartyName);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.NextMethod);
        Assert.Equal(nextParty.Id, movement.NextPartyId);
        Assert.Equal("Receiving", movement.NextPartyName);
        Assert.Equal(OrderHistorySource.DeliveryRouteReassigned, movement.Source);
        Assert.Equal(reloaded.Version, movement.OrderVersion);
        Assert.Equal(trip.Id, movement.OldDeliveryTripId);
        Assert.Null(movement.OldShipmentId);
        Assert.Null(movement.NewDeliveryTripId);
        Assert.Null(movement.NewShipmentId);

        var tracked = await context.OrderDeliveryMovements.SingleAsync();
        context.Entry(tracked).Property(item => item.Reason).CurrentValue = "tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Reassigning_the_replacement_again_reuses_the_freed_rank_without_collision()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (client, seedUser, operatorUser, firstParty, secondParty, nextParty) =
            await SeedPartiesAsync(context);
        var carrier = new ShippingCarrier(
            "Carrier",
            25m,
            Phone.Create("0504444444", "+966").Value,
            Address.Create("King Fahd Road", firstParty.City).Value);
        context.ShippingCarriers.Add(carrier);
        await context.SaveChangesAsync();

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
        var repository = new OrderRepository(context);
        var operation = new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(5));
        Assert.True(order.ReassignDeliveryStep(first.Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(nextParty.Id, "Receiving"), "Original", operation, "First move").IsSuccess);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await repository.TombstoneDeliveryStepAsync(first, CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        context.ChangeTracker.Clear();
        var reloaded = await repository.GetForLifecycleAsync(order.Id);
        var replacement = reloaded.DeliverySteps.Single(step => step.StepOrder == 1);
        Assert.True(reloaded.TransitionStep(replacement.Id, DeliveryStatus.InProgress,
            new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(10))).IsSuccess);
        var replacementTrip = replacement.DeliveryTrips.Single();
        ((IWorkRecord)replacementTrip).AssignWorkRecordNumber(nextParty.Id, 1, 2026, 11);
        await context.SaveChangesAsync();
        var replacementId = replacement.Id;
        Assert.True(reloaded.ReassignDeliveryStep(replacementId, DeliveryMethod.ShippingCompany,
            new DeliveryParty(carrier.Id, "Carrier"), "Receiving",
            new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(15)), "Second move").IsSuccess);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await repository.TombstoneDeliveryStepAsync(replacement, CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        context.ChangeTracker.Clear();
        var final = await repository.GetForLifecycleAsync(order.Id);
        Assert.Equal(-first.Id, final.DeliverySteps.Single(step => step.Id == first.Id).StepOrder);
        Assert.Equal(-replacementId, final.DeliverySteps.Single(step => step.Id == replacementId).StepOrder);
        Assert.Equal(2, final.DeliverySteps.Single(step => step.Id == second.Id).StepOrder);
        var latest = final.DeliverySteps.Single(step =>
            step.Id != first.Id && step.Id != replacementId && step.Id != second.Id);
        Assert.Equal(1, latest.StepOrder);
        Assert.Equal(carrier.Id, latest.ShippingCarrierId);
        Assert.Equal(DeliveryStatus.Pending, latest.Status);

        var movements = await context.OrderDeliveryMovements.AsNoTracking()
            .OrderBy(movement => movement.Id).ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.Equal(replacementId, movements[1].DeliveryStepId);
        Assert.Equal(1, movements[1].OriginalStepRank);
        Assert.Equal(latest.Id, movements[1].ReplacementStepId);
        Assert.Equal("Carrier", movements[1].NextPartyName);
    }

    [Fact]
    public async Task Direct_reassignment_switches_route_and_links_the_new_work_record()
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
        var seed = new OrderOperation(seedUser.Id, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        var trip = order.DeliveryTrips.Single();
        ((IWorkRecord)trip).AssignWorkRecordNumber(firstParty.Id, 1, 2026, 11);
        await context.SaveChangesAsync();

        var operation = new OrderOperation(operatorUser.Id, "Operator", AtUtc.AddMinutes(5));
        Assert.True(order.ReassignDirectDelivery(DeliveryMethod.ShippingCompany,
            new DeliveryParty(carrier.Id, "Carrier"), "Original", operation, "Switch route").IsSuccess);
        var shipment = order.Shipments.Single();
        ((IWorkRecord)shipment).AssignWorkRecordNumber(carrier.Id, 1, 2026, 11);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var repository = new OrderRepository(context);
        var reloaded = await repository.GetForLifecycleAsync(order.Id);
        Assert.Equal(DeliveryMethod.ShippingCompany, reloaded.DeliveryMethod);
        Assert.Equal(OrderStatus.Shipped, reloaded.OrderStatus);
        Assert.Null(reloaded.DeliverymanId);
        Assert.Equal(carrier.Id, reloaded.ShippingCarrierId);

        var savedTrip = await context.DeliveryTrips.AsNoTracking().SingleAsync(record => record.Id == trip.Id);
        Assert.Equal(WorkRecordStatus.Cancelled, savedTrip.Status);
        Assert.Equal(firstParty.Id, savedTrip.NumberingPartyId);
        var savedShipment = await context.Shipments.AsNoTracking().SingleAsync(record => record.Id == shipment.Id);
        Assert.Equal(WorkRecordStatus.Pending, savedShipment.Status);
        Assert.Equal(carrier.Id, savedShipment.NumberingPartyId);

        var movement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Null(movement.DeliveryStepId);
        Assert.Null(movement.ReplacementStepId);
        Assert.Null(movement.OriginalStepRank);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.PreviousMethod);
        Assert.Equal(firstParty.Id, movement.PreviousPartyId);
        Assert.Equal("Original", movement.PreviousPartyName);
        Assert.Equal(DeliveryMethod.ShippingCompany, movement.NextMethod);
        Assert.Equal(carrier.Id, movement.NextPartyId);
        Assert.Equal("Carrier", movement.NextPartyName);
        Assert.Equal(trip.Id, movement.OldDeliveryTripId);
        Assert.Equal(shipment.Id, movement.NewShipmentId);
        Assert.Null(movement.OldShipmentId);
        Assert.Null(movement.NewDeliveryTripId);
    }

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
        var order = new Order(client.Id, "ORD-REASSIGN");
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
