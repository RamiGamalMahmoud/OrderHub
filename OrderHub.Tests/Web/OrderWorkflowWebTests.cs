using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using MudBlazor.Services;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Services;
using OrderHub.Web.Components.Pages.Features.Orders.Workflow;

namespace OrderHub.Tests.Web;

public class OrderWorkflowWebTests
{
    private static OrderWorkflowState State(bool archived = false, OrderStatus status = OrderStatus.OutForDelivery) =>
        new(77, "ORD-WEB", DateTime.UtcNow, status, DeliveryMethod.DeliveryChain, null, 2, false, false, archived,
            null, archived || status == OrderStatus.Delivered ? [] : [OrderStatus.Cancelled],
            [new(1, 10, DeliveryMethod.DeliveryMan, "Courier", DeliveryStatus.Failed,
                archived || status == OrderStatus.Delivered ? [] : [DeliveryStatus.InProgress],
                archived || status == OrderStatus.Delivered ? "أُغلق تنفيذ الخطوة مع الطلب." : null,
                DateTime.UtcNow, "Operator", "Client absent", false),
             new(2, 20, DeliveryMethod.DeliveryMan, "Courier", DeliveryStatus.Pending, [],
                "بانتظار إكمال الخطوة السابقة", null, null, null, false)],
            DateTime.UtcNow, null, false, null, null, null);

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Chain_screen_shows_all_steps_and_locks_without_order_failure_action(bool writer, bool archived)
    {
        var html = await Render(State(archived), writer);
        Assert.Contains("الخطوة 10", html);
        Assert.Contains("الخطوة 20", html);
        Assert.Contains("بانتظار إكمال الخطوة السابقة", html);
        Assert.Contains("Client absent", html);
        Assert.DoesNotContain("تسجيل تعذر التسليم", html);
        Assert.Equal(writer && !archived, html.Contains("إعادة المحاولة"));
        if (archived) Assert.Contains("مؤرشف", html);
        if (!writer) Assert.Contains("العرض للقراءة فقط", html);
    }

    [Fact]
    public async Task Delivered_screen_keeps_incomplete_steps_visible_without_execution_actions()
    {
        var html = await Render(State(status: OrderStatus.Delivered), true);
        Assert.Contains("تم تسليم الطلب مع خطوات غير مكتملة", html);
        Assert.Contains("أُغلق تنفيذ الخطوة مع الطلب", html);
        Assert.DoesNotContain("إعادة المحاولة", html);
        Assert.DoesNotContain("إلغاء الطلب</", html);
    }

    [Fact]
    public async Task Missing_and_failed_reads_show_clear_messages_instead_of_render_errors()
    {
        Assert.Contains("الطلب غير موجود", await Render(null, false));
        Assert.Contains("تعذر تحميل دورة الطلب", await Render(null, false, failRead: true));
    }

    private static async Task<string> Render(OrderWorkflowState state, bool writer, bool failRead = false)
    {
        var executor = new Mock<IRequestExecutor>();
        var read = executor.Setup(value => value.ExecuteAsync(It.IsAny<GetOrderWorkflowQuery>(), It.IsAny<CancellationToken>()));
        if (failRead) read.ThrowsAsync(new InvalidOperationException("Unavailable"));
        else read.ReturnsAsync(state);
        executor.Setup(value => value.ExecuteAsync(It.IsAny<CheckPermissionQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(writer);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<IRequestExecutor>(executor.Object);
        services.AddSingleton(new PermissionAccess(executor.Object));
        services.AddSingleton<IJSRuntime>(new Mock<IJSRuntime>().Object);
        services.AddSingleton<NavigationManager>(new TestNavigation());
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<OrderWorkflow>(ParameterView.FromDictionary(new Dictionary<string, object> { ["Id"] = 77 }));
            return System.Net.WebUtility.HtmlDecode(root.ToHtmlString());
        });
    }

    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation() => Initialize("http://localhost/", "http://localhost/orders/77/workflow");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
}
