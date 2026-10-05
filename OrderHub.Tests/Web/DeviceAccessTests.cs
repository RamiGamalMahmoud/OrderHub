using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrderHub.Web.Infrastructure.DeviceAccess;

namespace OrderHub.Tests.Web;

public class DeviceAccessTests
{
    private const string WindowsChrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
    private const string Iphone = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1";
    private const string Ipad = "Mozilla/5.0 (iPad; CPU OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1";

    [Theory]
    [InlineData(WindowsChrome, DeviceType.Desktop)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0", DeviceType.Desktop)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Safari/605.1.15", DeviceType.Desktop)]
    [InlineData(Iphone, DeviceType.Mobile)]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36", DeviceType.Mobile)]
    [InlineData(Ipad, DeviceType.Tablet)]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-X710) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36", DeviceType.Tablet)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15) AppleWebKit/605.1.15 Version/18.1 Mobile/15E148 Safari/604.1", DeviceType.Tablet)]
    [InlineData("", DeviceType.Desktop)]
    [InlineData(null, DeviceType.Desktop)]
    [InlineData("UnknownClient/1.0", DeviceType.Desktop)]
    public void Detector_classifies_known_devices_and_fails_open(string userAgent, DeviceType expected)
    {
        var context = Context(userAgent);
        Assert.Equal(expected, new UserAgentDeviceDetector().Detect(context.Request));
    }

    [Theory]
    [InlineData(Iphone)]
    [InlineData(Ipad)]
    public async Task Blocked_device_gets_standalone_arabic_403_without_calling_next(string userAgent)
    {
        var context = Context(userAgent);
        context.Response.Body = new MemoryStream();
        var called = false;
        var middleware = new DeviceAccessMiddleware(_ => { called = true; return Task.CompletedTask; }, new UserAgentDeviceDetector());
        await middleware.InvokeAsync(context);
        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
        context.Response.Body.Position = 0;
        var html = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("هذا الجهاز غير مدعوم", html);
        Assert.Contains("المكتبية والمحمولة فقط", html);
        Assert.Contains("lang=\"ar\" dir=\"rtl\"", html);
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("<link", html);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/_blazor/negotiate")]
    [InlineData("/_blazor")]
    [InlineData("/_framework/blazor.web.js")]
    [InlineData("/_content/MudBlazor/MudBlazor.min.css")]
    public async Task Desktop_application_framework_and_asset_requests_continue_unchanged(string path)
    {
        var context = Context(WindowsChrome);
        context.Request.Path = path;
        var called = false;
        var middleware = new DeviceAccessMiddleware(_ =>
        {
            called = true;
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.CompletedTask;
        }, new UserAgentDeviceDetector());
        await middleware.InvokeAsync(context);
        Assert.True(called);
        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UnknownClient/1.0")]
    public async Task Missing_and_unknown_user_agents_are_allowed(string userAgent)
    {
        var called = false;
        var middleware = new DeviceAccessMiddleware(_ => { called = true; return Task.CompletedTask; }, new UserAgentDeviceDetector());
        await middleware.InvokeAsync(Context(userAgent));
        Assert.True(called);
    }

    private static DefaultHttpContext Context(string userAgent)
    {
        var context = new DefaultHttpContext();
        if (userAgent is not null)
            context.Request.Headers.UserAgent = userAgent;
        return context;
    }
}
