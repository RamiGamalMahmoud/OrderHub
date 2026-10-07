using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Application;

public class OrderLifecycleHandlerTests
{
    [Fact]
    public async Task Trusted_identity_and_clock_are_recorded_and_valid_change_saves_once()
    {
        var root = new Order(1, "TEST");
        root.UpdateHeader(1, OrderSource.Online, DeliveryMethod.Pickup, null, null, null);
        var repository = new Mock<IOrderRepository>(MockBehavior.Strict);
        repository.Setup(repo => repo.GetForLifecycleAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(root);
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unit.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var at = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var handler = new OrderLifecycleHandlers(repository.Object, unit.Object, AuthorizationTests.Identity(true, 7, false), new FixedClock(at));
        var result = await handler.Handle(new ChangeOrderStatusCommand(1, 1, OrderStatus.ReadyForPickup), default);
        Assert.True(result.IsSuccess);
        var history = Assert.Single(root.StatusHistory);
        Assert.Equal(at, history.ChangedAtUtc);
        Assert.Equal(7, history.ChangedByUserId);
        Assert.Equal("Operator", history.ChangedByUserName);
        unit.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.True((await handler.Handle(new ChangeOrderStatusCommand(1, 2, OrderStatus.ReadyForPickup), default)).IsSuccess);
        unit.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Conflict", (await handler.Handle(new ChangeOrderStatusCommand(1, 1, OrderStatus.Delivered), default)).ErrorCode);
    }

    [Fact]
    public async Task Unauthorized_and_invalid_operations_save_nothing()
    {
        var root = new Order(1, "TEST");
        var repository = new Mock<IOrderRepository>(MockBehavior.Strict);
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var anonymous = new OrderLifecycleHandlers(repository.Object, unit.Object, AuthorizationTests.Identity(false, null, false), TimeProvider.System);
        Assert.Equal("Unauthorized", (await anonymous.Handle(new ChangeOrderStatusCommand(1, 1, OrderStatus.Cancelled, "Cancel"), default)).ErrorCode);
        repository.Setup(repo => repo.GetForLifecycleAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(root);
        var writer = new OrderLifecycleHandlers(repository.Object, unit.Object, AuthorizationTests.Identity(true, 7, false), TimeProvider.System);
        Assert.False((await writer.Handle(new ChangeOrderStatusCommand(1, 1, OrderStatus.Delivered), default)).IsSuccess);
        Assert.False((await writer.Handle(new ChangeOrderStatusCommand(1, 1, OrderStatus.Cancelled, " "), default)).IsSuccess);
        Assert.False((await writer.Handle(new ChangeDeliveryStepStatusCommand(1, 1, 999, DeliveryStatus.Completed), default)).IsSuccess);
        unit.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(root.StatusHistory);
    }

    private sealed class FixedClock(DateTime at) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(at);
    }
}
