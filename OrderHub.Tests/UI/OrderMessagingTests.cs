using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Moq;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.Orders.ChangePaymentMethod;
using OrderHub.Application.Features.Orders.ChangeStatus;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Features.Orders;
using OrderHub.UI.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests.UI;

public class OrderMessagingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StatusMessageIsSentOnlyAfterSuccess(bool succeeds)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(
                It.Is<IRequest<Result<OrderStatus>>>(r => r is ShipOrderCommand),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(succeeds
                ? Result<OrderStatus>.Success(OrderStatus.Shipped)
                : Result<OrderStatus>.Failure("Rejected"));
        var recipient = new object();
        OrdersMessages.OrderStatusChangedMessage received = null;
        WeakReferenceMessenger.Default.Register<OrdersMessages.OrderStatusChangedMessage>(
            recipient, (_, message) => received = message);
        try
        {
            CreateOrder(executor.Object).OrderStatus = new EnumItem<OrderStatus>(OrderStatus.Shipped, "Shipped");
            Assert.Equal(succeeds ? new OrdersMessages.OrderStatusChangedMessage(42, OrderStatus.Shipped) : null, received);
            executor.VerifyAll();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PaymentMethodMessageIsSentOnlyAfterSuccess(bool succeeds)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(
                It.Is<IRequest<Result>>(r => r is ChangePaymentMethodCommand),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(succeeds ? Result.Success() : Result.Failure("Rejected"));
        var recipient = new object();
        OrdersMessages.OrderPaymentMethodChangedMessage received = null;
        WeakReferenceMessenger.Default.Register<OrdersMessages.OrderPaymentMethodChangedMessage>(
            recipient, (_, message) => received = message);
        try
        {
            CreateOrder(executor.Object).PaymentMethod = new PaymentMethod(7, "Cash", "", true);
            Assert.Equal(succeeds ? new OrdersMessages.OrderPaymentMethodChangedMessage(42, 7) : null, received);
            executor.VerifyAll();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [Fact]
    public async Task PendingStatusOperationDoesNotPublishPrematurely()
    {
        var completion = new TaskCompletionSource<Result<OrderStatus>>();
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(
                It.IsAny<IRequest<Result<OrderStatus>>>(), It.IsAny<CancellationToken>()))
            .Returns(completion.Task);
        var recipient = new object();
        var received = new TaskCompletionSource<OrdersMessages.OrderStatusChangedMessage>();
        WeakReferenceMessenger.Default.Register<OrdersMessages.OrderStatusChangedMessage>(
            recipient, (_, message) => received.TrySetResult(message));
        try
        {
            CreateOrder(executor.Object).OrderStatus = new EnumItem<OrderStatus>(OrderStatus.Shipped, "Shipped");
            Assert.False(received.Task.IsCompleted);
            completion.SetResult(Result<OrderStatus>.Success(OrderStatus.Shipped));
            Assert.Equal(new OrdersMessages.OrderStatusChangedMessage(42, OrderStatus.Shipped),
                await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    private static OrderViewModel CreateOrder(IRequestExecutor executor) => new(
        executor, 42, "ORD-42", "Client", "", 1, 100m, DateTime.UtcNow,
        new PaymentMethod(1, "Original", "", true),
        new EnumItem<OrderStatus>(OrderStatus.Pending, "Pending"),
        false, false, false, false, false, false, false, false);
}
