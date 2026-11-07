using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Reopsitories;

namespace OrderHub.Tests;

public sealed class OrderReassignmentPersistenceTests
{
    private static readonly DateTime AtUtc = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Chain_reassignment_tombstones_the_old_step_and_persists_the_movement()
    {
        using var connection = OpenConnection();
        await using var context = new AppDbContext(Options(connection));
        context.Database.EnsureCreated();
        var order = NewChainOrder(7, 9);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var first = order.DeliverySteps.OrderBy(step => step.StepOrder).First();
        var second = order.DeliverySteps.OrderBy(step => step.StepOrder).Last();
        var seed = new OrderOperation(1, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        Assert.True(order.TransitionStep(first.Id, DeliveryStatus.InProgress, seed).IsSuccess);
        var trip = first.DeliveryTrips.Single();
        ((IWorkRecord)trip).AssignWorkRecordNumber(7, 1, 2026, 11);
        await context.SaveChangesAsync();
        var oldStepId = first.Id;
        var secondStepId = second.Id;

        var operation = new OrderOperation(2, "Operator", AtUtc.AddMinutes(5));
        var reassignment = order.ReassignDeliveryStep(oldStepId, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Receiving"), "Original", operation, "Moved");
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
        Assert.Equal(8, replacement.DeliverymanId);
        Assert.Equal(DeliveryStatus.Pending, replacement.Status);
        Assert.Equal(2, reloaded.DeliverySteps.Single(step => step.Id == secondStepId).StepOrder);

        var savedTrip = await context.DeliveryTrips.AsNoTracking().SingleAsync(record => record.Id == trip.Id);
        Assert.Equal(WorkRecordStatus.Cancelled, savedTrip.Status);
        Assert.Equal(7, savedTrip.NumberingPartyId);
        Assert.Equal(1L, savedTrip.WorkRecordNumber);

        var movement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Equal(order.Id, movement.OrderId);
        Assert.Equal(oldStepId, movement.DeliveryStepId);
        Assert.Equal(replacement.Id, movement.ReplacementStepId);
        Assert.Equal(1, movement.OriginalStepRank);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.PreviousMethod);
        Assert.Equal(7, movement.PreviousPartyId);
        Assert.Equal("Original", movement.PreviousPartyName);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.NextMethod);
        Assert.Equal(8, movement.NextPartyId);
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
        using var connection = OpenConnection();
        await using var context = new AppDbContext(Options(connection));
        context.Database.EnsureCreated();
        var order = NewChainOrder(7, 9);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var first = order.DeliverySteps.OrderBy(step => step.StepOrder).First();
        var second = order.DeliverySteps.OrderBy(step => step.StepOrder).Last();
        var seed = new OrderOperation(1, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        Assert.True(order.TransitionStep(first.Id, DeliveryStatus.InProgress, seed).IsSuccess);
        ((IWorkRecord)first.DeliveryTrips.Single()).AssignWorkRecordNumber(7, 1, 2026, 11);
        await context.SaveChangesAsync();
        var repository = new OrderRepository(context);
        var operation = new OrderOperation(2, "Operator", AtUtc.AddMinutes(5));
        Assert.True(order.ReassignDeliveryStep(first.Id, DeliveryMethod.DeliveryMan,
            new DeliveryParty(8, "Receiving"), "Original", operation, "First move").IsSuccess);
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
            new OrderOperation(2, "Operator", AtUtc.AddMinutes(10))).IsSuccess);
        var replacementTrip = replacement.DeliveryTrips.Single();
        ((IWorkRecord)replacementTrip).AssignWorkRecordNumber(8, 1, 2026, 11);
        await context.SaveChangesAsync();
        var replacementId = replacement.Id;
        Assert.True(reloaded.ReassignDeliveryStep(replacementId, DeliveryMethod.ShippingCompany,
            new DeliveryParty(20, "Carrier"), "Receiving",
            new OrderOperation(2, "Operator", AtUtc.AddMinutes(15)), "Second move").IsSuccess);
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
        Assert.Equal(20, latest.ShippingCarrierId);
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
        using var connection = OpenConnection();
        await using var context = new AppDbContext(Options(connection));
        context.Database.EnsureCreated();
        var order = new Order(10, "ORD-DIRECT");
        order.UpdateHeader(10, OrderSource.Online, DeliveryMethod.DeliveryMan, 7, null, null);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var seed = new OrderOperation(1, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        var trip = order.DeliveryTrips.Single();
        ((IWorkRecord)trip).AssignWorkRecordNumber(7, 1, 2026, 11);
        await context.SaveChangesAsync();

        var operation = new OrderOperation(2, "Operator", AtUtc.AddMinutes(5));
        Assert.True(order.ReassignDirectDelivery(DeliveryMethod.ShippingCompany,
            new DeliveryParty(20, "Carrier"), "Original", operation, "Switch route").IsSuccess);
        var shipment = order.Shipments.Single();
        ((IWorkRecord)shipment).AssignWorkRecordNumber(20, 1, 2026, 11);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var repository = new OrderRepository(context);
        var reloaded = await repository.GetForLifecycleAsync(order.Id);
        Assert.Equal(DeliveryMethod.ShippingCompany, reloaded.DeliveryMethod);
        Assert.Equal(OrderStatus.Shipped, reloaded.OrderStatus);
        Assert.Null(reloaded.DeliverymanId);
        Assert.Equal(20, reloaded.ShippingCarrierId);

        var savedTrip = await context.DeliveryTrips.AsNoTracking().SingleAsync(record => record.Id == trip.Id);
        Assert.Equal(WorkRecordStatus.Cancelled, savedTrip.Status);
        Assert.Equal(7, savedTrip.NumberingPartyId);
        var savedShipment = await context.Shipments.AsNoTracking().SingleAsync(record => record.Id == shipment.Id);
        Assert.Equal(WorkRecordStatus.Pending, savedShipment.Status);
        Assert.Equal(20, savedShipment.NumberingPartyId);

        var movement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Null(movement.DeliveryStepId);
        Assert.Null(movement.ReplacementStepId);
        Assert.Null(movement.OriginalStepRank);
        Assert.Equal(DeliveryMethod.DeliveryMan, movement.PreviousMethod);
        Assert.Equal(7, movement.PreviousPartyId);
        Assert.Equal("Original", movement.PreviousPartyName);
        Assert.Equal(DeliveryMethod.ShippingCompany, movement.NextMethod);
        Assert.Equal(20, movement.NextPartyId);
        Assert.Equal("Carrier", movement.NextPartyName);
        Assert.Equal(trip.Id, movement.OldDeliveryTripId);
        Assert.Equal(shipment.Id, movement.NewShipmentId);
        Assert.Null(movement.OldShipmentId);
        Assert.Null(movement.NewDeliveryTripId);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = OFF";
        command.ExecuteNonQuery();
        return connection;
    }

    private static DbContextOptions<AppDbContext> Options(SqliteConnection connection) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

    private static Order NewChainOrder(int firstPartyId, int secondPartyId)
    {
        var order = new Order(10, "ORD-REASSIGN");
        order.UpdateHeader(10, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
        order.AddDeliveryStep(new OrderDeliveryStep
        {
            StepOrder = 1,
            DeliveryMethod = DeliveryMethod.DeliveryMan,
            DeliverymanId = firstPartyId
        });
        order.AddDeliveryStep(new OrderDeliveryStep
        {
            StepOrder = 2,
            DeliveryMethod = DeliveryMethod.DeliveryMan,
            DeliverymanId = secondPartyId
        });
        return order;
    }
}
