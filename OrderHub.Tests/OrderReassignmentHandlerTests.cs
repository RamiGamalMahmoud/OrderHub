using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Orders.Reassignment;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Reopsitories;

namespace OrderHub.Tests;

public sealed class OrderReassignmentHandlerTests
{
    private static readonly DateTime AtUtc = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Step_reassignment_requires_an_authenticated_actor()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (order, stepId, _, nextPartyId) = await SeedChainAsync(context);

        var result = await NewHandler(context, authenticated: false).Handle(
            new ReassignDeliveryStepCommand(order.Id, order.Version, stepId, DeliveryMethod.DeliveryMan,
                nextPartyId, "Move"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.ErrorCode);
    }

    [Fact]
    public async Task Step_reassignment_rejects_a_stale_expected_version()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (order, stepId, _, nextPartyId) = await SeedChainAsync(context);
        var version = order.Version;

        var result = await NewHandler(context).Handle(
            new ReassignDeliveryStepCommand(order.Id, version - 1, stepId, DeliveryMethod.DeliveryMan,
                nextPartyId, "Move"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Empty(context.OrderDeliveryMovements);
    }

    [Fact]
    public async Task Step_reassignment_rejects_an_unknown_party_without_writing()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (order, stepId, _, _) = await SeedChainAsync(context);
        var version = order.Version;

        var result = await NewHandler(context).Handle(
            new ReassignDeliveryStepCommand(order.Id, version, stepId, DeliveryMethod.ShippingCompany,
                999, "Move"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
        Assert.Empty(context.OrderDeliveryMovements);
        Assert.Equal(version, (await context.Orders.AsNoTracking().SingleAsync(item => item.Id == order.Id)).Version);
    }

    [Fact]
    public async Task Step_reassignment_rejects_the_current_party_without_writing()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (order, stepId, firstPartyId, _) = await SeedChainAsync(context);
        var version = order.Version;

        var result = await NewHandler(context).Handle(
            new ReassignDeliveryStepCommand(order.Id, version, stepId, DeliveryMethod.DeliveryMan,
                firstPartyId, "Move"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidOperation", result.ErrorCode);
        Assert.Empty(context.OrderDeliveryMovements);
        Assert.Equal(version, (await context.Orders.AsNoTracking().SingleAsync(item => item.Id == order.Id)).Version);
        Assert.Equal(DeliveryStatus.InProgress,
            (await context.OrderDeliverySteps.AsNoTracking().SingleAsync(step => step.Id == stepId)).Status);
    }

    [Fact]
    public async Task Step_reassignment_saves_the_replacement_and_the_movement()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (order, stepId, firstPartyId, nextPartyId) = await SeedChainAsync(context);
        var version = order.Version;

        var result = await NewHandler(context).Handle(
            new ReassignDeliveryStepCommand(order.Id, version, stepId, DeliveryMethod.DeliveryMan,
                nextPartyId, "Move"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(version + 1, result.Value.Version);
        context.ChangeTracker.Clear();
        var reloaded = await context.Orders.Include(item => item.DeliverySteps)
            .SingleAsync(item => item.Id == order.Id);
        Assert.Equal(version + 1, reloaded.Version);
        Assert.Equal(-stepId, reloaded.DeliverySteps.Single(step => step.Id == stepId).StepOrder);
        var replacement = reloaded.DeliverySteps.Single(step => step.Id != stepId && step.StepOrder == 1);
        Assert.Equal(1, replacement.StepOrder);
        Assert.Equal(nextPartyId, replacement.DeliverymanId);
        Assert.Equal(DeliveryStatus.Pending, replacement.Status);
        Assert.NotEqual(firstPartyId, replacement.DeliverymanId);
        var movement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Equal(replacement.Id, movement.ReplacementStepId);
        Assert.Equal(nextPartyId, movement.NextPartyId);
        Assert.Equal(firstPartyId, movement.PreviousPartyId);
        Assert.Equal(version + 1, movement.OrderVersion);
    }

    [Fact]
    public async Task Cancelling_a_step_saves_the_tombstone_without_a_replacement()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (order, stepId, firstPartyId, _) = await SeedChainAsync(context);
        var version = order.Version;

        var result = await NewHandler(context).Handle(
            new CancelDeliveryStepCommand(order.Id, version, stepId, "No replacement"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(version + 1, result.Value.Version);
        context.ChangeTracker.Clear();
        var reloaded = await context.Orders.Include(item => item.DeliverySteps)
            .ThenInclude(step => step.DeliveryTrips)
            .SingleAsync(item => item.Id == order.Id);
        var cancelled = reloaded.DeliverySteps.Single(step => step.Id == stepId);
        Assert.Equal(-stepId, cancelled.StepOrder);
        Assert.Equal(DeliveryStatus.Cancelled, cancelled.Status);
        Assert.Equal(WorkRecordStatus.Cancelled, cancelled.DeliveryTrips.Single().Status);
        Assert.Equal(2, await context.OrderDeliverySteps.CountAsync());
        var movement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Equal(stepId, movement.DeliveryStepId);
        Assert.Null(movement.ReplacementStepId);
        Assert.Null(movement.NextMethod);
        Assert.Null(movement.NextPartyId);
        Assert.Null(movement.NextPartyName);
        Assert.Equal(firstPartyId, movement.PreviousPartyId);
        Assert.Equal(1, movement.OriginalStepRank);
        Assert.Equal(OrderHistorySource.DeliveryStepCancelled, movement.Source);
        Assert.Equal(version + 1, movement.OrderVersion);
    }

    [Fact]
    public async Task Direct_reassignment_creates_a_record_for_the_new_party()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (order, firstPartyId, nextPartyId) = await SeedDirectAsync(context);
        var version = order.Version;

        var result = await NewHandler(context).Handle(
            new ReassignDirectDeliveryCommand(order.Id, version, DeliveryMethod.DeliveryMan,
                nextPartyId, "Move"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        context.ChangeTracker.Clear();
        var reloaded = await context.Orders
            .Include(item => item.DeliveryTrips)
            .SingleAsync(item => item.Id == order.Id);
        Assert.Equal(DeliveryMethod.DeliveryMan, reloaded.DeliveryMethod);
        Assert.Equal(OrderStatus.OutForDelivery, reloaded.OrderStatus);
        Assert.Equal(nextPartyId, reloaded.DeliverymanId);
        Assert.Equal(WorkRecordStatus.Cancelled, reloaded.DeliveryTrips.Single(trip => trip.NumberingPartyId == firstPartyId).Status);
        var current = reloaded.DeliveryTrips.Single(trip => trip.NumberingPartyId == nextPartyId);
        Assert.Equal(WorkRecordStatus.Pending, current.Status);
        var movement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Equal(current.Id, movement.NewDeliveryTripId);
        Assert.Null(movement.NewShipmentId);
        Assert.Equal(nextPartyId, movement.NextPartyId);
        Assert.Equal(version + 1, movement.OrderVersion);
    }

    [Fact]
    public async Task Direct_reassignment_to_pickup_moves_the_order_without_a_new_record()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (order, firstPartyId, _) = await SeedDirectAsync(context);
        var version = order.Version;
        var tripsBefore = await context.DeliveryTrips.CountAsync();

        var result = await NewHandler(context).Handle(
            new ReassignDirectDeliveryCommand(order.Id, version, DeliveryMethod.Pickup,
                null, "Customer pickup"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        context.ChangeTracker.Clear();
        var reloaded = await context.Orders.Include(item => item.DeliveryTrips)
            .SingleAsync(item => item.Id == order.Id);
        Assert.Equal(DeliveryMethod.Pickup, reloaded.DeliveryMethod);
        Assert.Equal(OrderStatus.ReadyForPickup, reloaded.OrderStatus);
        Assert.Null(reloaded.DeliverymanId);
        Assert.Null(reloaded.ShippingCarrierId);
        Assert.Equal(WorkRecordStatus.Cancelled, reloaded.DeliveryTrips.Single().Status);
        Assert.Equal(firstPartyId, reloaded.DeliveryTrips.Single().NumberingPartyId);
        Assert.Equal(tripsBefore, await context.DeliveryTrips.CountAsync());
        Assert.Equal(0, await context.Shipments.CountAsync());
        var movement = await context.OrderDeliveryMovements.AsNoTracking().SingleAsync();
        Assert.Equal(DeliveryMethod.Pickup, movement.NextMethod);
        Assert.Null(movement.NextPartyId);
        Assert.Null(movement.NewDeliveryTripId);
        Assert.Null(movement.NewShipmentId);
    }

    private static OrderReassignmentHandlers NewHandler(AppDbContext context, bool authenticated = true) =>
        new(new OrderRepository(context), new TestUnitOfWork(context), new TestUser(authenticated),
            TimeProvider.System);

    private static async Task<(Order Order, int StepId, int FirstPartyId, int NextPartyId)> SeedChainAsync(
        AppDbContext context)
    {
        var (client, user, first, second, next) = await SeedMasterAndPartiesAsync(context);
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
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var step = order.DeliverySteps.OrderBy(item => item.StepOrder).First();
        var seed = new OrderOperation(user.Id, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        Assert.True(order.TransitionStep(step.Id, DeliveryStatus.InProgress, seed).IsSuccess);
        ((IWorkRecord)step.DeliveryTrips.Single()).AssignWorkRecordNumber(first.Id, 1, 2026, 11);
        await context.SaveChangesAsync();
        return (order, step.Id, first.Id, next.Id);
    }

    private static async Task<(Order Order, int FirstPartyId, int NextPartyId)> SeedDirectAsync(AppDbContext context)
    {
        var (client, user, first, _, next) = await SeedMasterAndPartiesAsync(context);
        var order = new Order(client.Id, "ORD-DIRECT");
        order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.DeliveryMan, first.Id, null, null);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var seed = new OrderOperation(user.Id, "Seed", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, seed).IsSuccess);
        ((IWorkRecord)order.DeliveryTrips.Single()).AssignWorkRecordNumber(first.Id, 1, 2026, 11);
        await context.SaveChangesAsync();
        return (order, first.Id, next.Id);
    }

    private static async Task<(Client Client, User User, Deliveryman First, Deliveryman Second, Deliveryman Next)>
        SeedMasterAndPartiesAsync(AppDbContext context)
    {
        var city = new City("Riyadh");
        var client = new Client("Client", "Street", city, "0501111111", "+966");
        var user = User.Create("tester", "hash", "Tester");
        var first = new Deliveryman("First", city, "0500000001");
        var second = new Deliveryman("Second", city, "0500000002");
        var next = new Deliveryman("Third", city, "0500000003");
        context.Cities.Add(city);
        context.Clients.Add(client);
        context.Users.Add(user);
        context.Deliverymen.AddRange(first, second, next);
        await context.SaveChangesAsync();
        return (client, user, first, second, next);
    }

    private sealed class TestUser(bool authenticated) : ICurrentUser
    {
        public int? Id => authenticated ? 1 : null;
        public string UserName => "tester";
        public string DisplayName => "Tester";
        public bool IsAuthenticated => authenticated;
        public bool IsAdmin => false;
    }

    private sealed class TestUnitOfWork(AppDbContext context) : IUnitOfWork
    {
        private IDbContextTransaction? transaction;
        private long lastNumber;

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var pending = context.ChangeTracker.Entries<IWorkRecord>()
                .Where(entry => entry.State == EntityState.Added && !entry.Entity.HasWorkRecordNumber)
                .Select(entry => entry.Entity)
                .ToList();
            foreach (var record in pending)
                record.AssignWorkRecordNumber(record.NumberingPartyId, ++lastNumber, 2026, 11);
            return await context.SaveChangesAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await transaction!.CommitAsync(cancellationToken);
            await transaction.DisposeAsync();
            transaction = null;
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            await transaction!.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            transaction = null;
        }
    }
}
