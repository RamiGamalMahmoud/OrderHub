using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Web.Components.Pages.Features.Orders;

namespace OrderHub.Tests.Web;

public class DraftDrawerLifecycleTests
{
    [Fact]
    public async Task Closed_drawer_does_not_load_and_each_reopen_refreshes()
    {
        var service = new Mock<IDraftService>();
        service.Setup(x => x.GetAllAsync()).ReturnsAsync(Array.Empty<OrderDraftSummary>());
        using var drawer = Create(service);
        await Parameters(drawer);
        service.Verify(x => x.GetAllAsync(), Times.Never);
        drawer.IsOpen = true;
        await Parameters(drawer);
        await Parameters(drawer);
        service.Verify(x => x.GetAllAsync(), Times.Once);
        drawer.IsOpen = false;
        await Parameters(drawer);
        drawer.IsOpen = true;
        await Parameters(drawer);
        service.Verify(x => x.GetAllAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task Close_notifies_parent_instead_of_overwriting_parameter()
    {
        using var drawer = Create(new Mock<IDraftService>());
        drawer.IsOpen = true;
        bool? requested = null;
        drawer.IsOpenChanged = EventCallback.Factory.Create<bool>(new object(), value => requested = value);
        await (Task)typeof(OrderDrafts).GetMethod("OnCloseClicked", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(drawer, null);
        Assert.Equal(false, requested);
        Assert.True(drawer.IsOpen);
    }

    [Fact]
    public async Task Read_failure_is_contained_and_retry_succeeds()
    {
        var service = new Mock<IDraftService>();
        service.SetupSequence(x => x.GetAllAsync())
            .ThrowsAsync(new TimeoutException())
            .ReturnsAsync(Array.Empty<OrderDraftSummary>());
        using var drawer = Create(service);
        drawer.IsOpen = true;
        await Parameters(drawer);
        Assert.NotNull(Field(drawer, "_error"));
        Assert.False((bool)Field(drawer, "_busy"));
        await drawer.RefreshAsync();
        Assert.Null(Field(drawer, "_error"));
    }

    [Fact]
    public async Task Pending_refresh_is_serialized_and_disposal_rejects_late_results()
    {
        var pending = new TaskCompletionSource<System.Collections.Generic.IReadOnlyList<OrderDraftSummary>>();
        var service = new Mock<IDraftService>();
        service.Setup(x => x.GetAllAsync()).Returns(pending.Task);
        using var drawer = Create(service);
        drawer.IsOpen = true;
        var load = Parameters(drawer);
        await drawer.RefreshAsync();
        service.Verify(x => x.GetAllAsync(), Times.Once);
        drawer.Dispose();
        pending.SetResult(new[] { new OrderDraftSummary(Guid.NewGuid(), "Late", 1, DateTime.UtcNow) });
        await load;
        Assert.Empty((System.Collections.Generic.IReadOnlyList<OrderDraftSummary>)Field(drawer, "_drafts"));
        service.Verify(x => x.GetAllAsync(), Times.Once);
    }

    private static OrderDrafts Create(Mock<IDraftService> service)
    {
        var drawer = new OrderDrafts();
        typeof(OrderDrafts).GetProperty("DraftService", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(drawer, service.Object);
        typeof(OrderDrafts).GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(drawer, NullLogger<OrderDrafts>.Instance);
        return drawer;
    }
    private static Task Parameters(OrderDrafts drawer) => (Task)typeof(OrderDrafts)
        .GetMethod("OnParametersSetAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(drawer, null);
    private static object Field(OrderDrafts drawer, string name) => typeof(OrderDrafts)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(drawer)
        ?? typeof(OrderDrafts).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(drawer);
}
