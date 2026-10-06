using System.Collections.Generic;
using System.Net;
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
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Interfaces;
using OrderHub.Web.Components.Pages.Features.Categories.Components;
using OrderHub.Web.Services;
using CategoryIndex = OrderHub.Web.Components.Pages.Features.Categories.Index;

namespace OrderHub.Tests.Web;

public class CategoryRenderingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Index_renders_write_actions_only_for_authorized_users(bool canWrite)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetCategoriesResponse(true, [new(24, "Chairs", 3, 26)], [new(10, "Furniture")]));
        executor.Setup(x => x.ExecuteAsync(It.IsAny<CheckPermissionQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(canWrite);
        using var provider = Services(executor.Object);
        var html = await Render<CategoryIndex>(provider, new() { [nameof(CategoryIndex.ParentId)] = 10 });
        Assert.Contains("Chairs", html);
        Assert.Contains("/categories/24", html);
        Assert.Contains("/categories", html);
        Assert.Equal(canWrite, html.Contains("إضافة قسم فرعي"));
        Assert.Equal(canWrite, html.Contains("تعديل القسم"));
        Assert.Equal(canWrite, html.Contains("حذف القسم"));
        Assert.Equal(canWrite, html.Contains("/categories/create?parentId=10"));
        Assert.Equal(canWrite, html.Contains("/categories/24/edit"));
    }

    [Fact]
    public async Task Leaf_card_has_no_active_child_navigation()
    {
        using var provider = Services(new Mock<IRequestExecutor>().Object);
        var html = await Render<CategoryCard>(provider, new() { [nameof(CategoryCard.Category)] = new CategoryIndexItem(24, "Leaf", 0, 8) });
        Assert.Contains("لا توجد أقسام فرعية", html);
        Assert.DoesNotContain("href=\"/categories/24\"", html);
        Assert.Contains("disabled", html);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_index_explains_state_and_hides_add_for_read_only_user(bool canWrite)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetCategoriesResponse(true, [], []));
        executor.Setup(x => x.ExecuteAsync(It.IsAny<CheckPermissionQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(canWrite);
        using var provider = Services(executor.Object);
        var html = await Render<CategoryIndex>(provider, []);
        Assert.Contains("لم تتم إضافة أقسام بعد.", html);
        Assert.Equal(canWrite, html.Contains("/categories/create"));
    }

    private static ServiceProvider Services(IRequestExecutor executor)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<IJSRuntime>(new Mock<IJSRuntime>().Object);
        services.AddSingleton<NavigationManager>(new TestNavigation());
        services.AddSingleton(executor);
        services.AddSingleton<PermissionAccess>();
        return services.BuildServiceProvider();
    }

    private static async Task<string> Render<T>(ServiceProvider services, Dictionary<string, object> parameters) where T : IComponent
    {
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters));
            return WebUtility.HtmlDecode(result.ToHtmlString());
        });
    }

    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation() => Initialize("http://localhost/", "http://localhost/categories");
        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();
    }
}
