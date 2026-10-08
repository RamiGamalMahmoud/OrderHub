using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Cities;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Cities.Editor;
using OrderHub.Web.Infrastructure.Authorization;
using OrderHub.Web.Services;
using CityIndex = OrderHub.Web.Components.Pages.Features.Cities.Index;

namespace OrderHub.Tests.Web;

public class CityWebTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Index_shows_mutation_actions_only_with_write_access(bool canWrite)
    {
        var executor = ListExecutor(canWrite, [new(1, "Cairo")]);
        await using var fixture = new HtmlFixture(executor.Object);
        await fixture.Render<CityIndex>();

        string html = await fixture.Markup();

        Assert.Contains("Cairo", html);
        Assert.Equal(canWrite, html.Contains("إضافة مدينة"));
        Assert.Equal(canWrite, html.Contains("تعديل المدينة"));
        Assert.Equal(canWrite, html.Contains("حذف المدينة"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_index_explains_state_and_respects_create_access(bool canWrite)
    {
        var executor = ListExecutor(canWrite, []);
        await using var fixture = new HtmlFixture(executor.Object);
        await fixture.Render<CityIndex>();

        string html = await fixture.Markup();

        Assert.Contains("لم تتم إضافة مدن بعد.", html);
        Assert.Equal(canWrite, html.Contains("إضافة مدينة"));
    }

    [Fact]
    public async Task Failed_list_can_be_retried_and_search_filters_without_database_reads()
    {
        var executor = ListExecutor(false, [new(1, "Cairo"), new(2, "Giza")]);
        executor.SetupSequence(service => service.ExecuteAsync(
                It.IsAny<GetAllCitiesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException())
            .ReturnsAsync(new[] { new CityListItem(1, "Cairo"), new CityListItem(2, "Giza") });

        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<CityIndex>();

        Assert.Contains("تعذر تحميل المدن", await fixture.Markup());

        await fixture.Invoke(index, "LoadAsync");
        await fixture.SetField(index, "_searchTerm", " cairo ");

        string html = await fixture.Markup();

        Assert.Contains("Cairo", html);
        Assert.DoesNotContain("Giza", html);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllCitiesQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(null, "إضافة مدينة", "")]
    [InlineData(4, "تعديل المدينة", "Cairo")]
    public async Task Shared_dialog_initializes_create_and_edit_modes(int? cityId, string title, string name)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetCityForEditQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CityUpdateInput(4, "Cairo"));

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<CityEditorDialog>(new() { [nameof(CityEditorDialog.CityId)] = cityId });

        Assert.Contains(title, await fixture.Markup());
        Assert.Equal(name, GetField<CityEditorState>(dialog, "_state").Name);

        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetCityForEditQuery>(), It.IsAny<CancellationToken>()),
            cityId.HasValue ? Times.Once() : Times.Never());

        await fixture.Invoke(dialog, "Cancel");

        fixture.DialogInstance.Verify(instance => instance.Cancel(), Times.Once);
        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(4)]
    public async Task Dialog_saves_through_correct_command_and_closes_with_success(int? cityId)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetCityForEditQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CityUpdateInput(4, "Cairo"));
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<CreateCityCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<UpdateCityCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<CityEditorDialog>(new() { [nameof(CityEditorDialog.CityId)] = cityId });
        GetField<CityEditorState>(dialog, "_state").Name = "Alexandria";
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        fixture.DialogInstance.Verify(instance => instance.Close(
            It.Is<DialogResult>(result => !result.Canceled && Equals(result.Data, true))), Times.Once);

        if (cityId.HasValue)
        {
            executor.Verify(service => service.ExecuteAsync(
                It.Is<UpdateCityCommand>(command => command.CityUpdateInput.Id == 4 &&
                    command.CityUpdateInput.Name == "Alexandria"), It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            executor.Verify(service => service.ExecuteAsync(
                It.Is<CreateCityCommand>(command => command.CityCreateInput.Name == "Alexandria"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task Dialog_keeps_invalid_and_duplicate_input_open_then_allows_retry()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.SetupSequence(service => service.ExecuteAsync(
                It.IsAny<CreateCityCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("هذه المدينة موجودة بالفعل."))
            .ReturnsAsync(Result.Success());

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<CityEditorDialog>();

        await fixture.Invoke(dialog, "SaveAsync");

        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<CreateCityCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        GetField<CityEditorState>(dialog, "_state").Name = "Cairo";
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        Assert.Contains("هذه المدينة موجودة بالفعل.", await fixture.Markup());
        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Never);

        GetField<CityEditorState>(dialog, "_state").Name = "Giza";
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Once);
    }

    [Fact]
    public async Task Missing_city_dialog_explains_failure_and_disables_save()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetCityForEditQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CityUpdateInput)null);

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<CityEditorDialog>(new() { [nameof(CityEditorDialog.CityId)] = 4 });

        Assert.Contains("المدينة غير موجودة.", await fixture.Markup());

        await fixture.Invoke(dialog, "SaveAsync");

        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<UpdateCityCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Index_refreshes_after_successful_dialog_result_but_not_after_cancel()
    {
        var executor = ListExecutor(true, [new(1, "Cairo")]);
        var dialogs = new Mock<IDialogService>();
        var canceled = new Mock<IDialogReference>();
        canceled.SetupGet(reference => reference.Result).Returns(Task.FromResult(DialogResult.Cancel()));
        var saved = new Mock<IDialogReference>();
        saved.SetupGet(reference => reference.Result).Returns(Task.FromResult(DialogResult.Ok(true)));

        dialogs.SetupSequence(service => service.ShowAsync<CityEditorDialog>(
                It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(canceled.Object)
            .ReturnsAsync(saved.Object);

        await using var fixture = new HtmlFixture(executor.Object, dialogs.Object);
        var index = await fixture.Render<CityIndex>();

        await fixture.Invoke(index, "OpenCreateDialogAsync");
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllCitiesQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        await fixture.Invoke(index, "OpenEditDialogAsync", new CityListItem(1, "Cairo"));
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllCitiesQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Dialog_recovers_from_load_failure_and_handles_unexpected_save_failure_and_denial()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.SetupSequence(service => service.ExecuteAsync(
                It.IsAny<GetCityForEditQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException())
            .ReturnsAsync(new CityUpdateInput(4, "Cairo"));
        executor.SetupSequence(service => service.ExecuteAsync(
                It.IsAny<UpdateCityCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException())
            .ThrowsAsync(new AuthorizationDeniedException());

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<CityEditorDialog>(new() { [nameof(CityEditorDialog.CityId)] = 4 });

        Assert.Contains("تعذر تحميل بيانات المدينة", await fixture.Markup());

        await fixture.Invoke(dialog, "LoadAsync");
        await fixture.Invoke(dialog, "SaveAsync");

        Assert.Contains("تعذر حفظ المدينة", await fixture.Markup());
        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Never);

        await fixture.Invoke(dialog, "SaveAsync");

        fixture.DialogInstance.Verify(instance => instance.Cancel(), Times.Once);
        Assert.EndsWith("/access-denied", fixture.NavigationUri);
    }

    [Fact]
    public async Task Delete_requires_confirmation_and_refreshes_only_after_success()
    {
        var executor = ListExecutor(true, [new(1, "Cairo")]);
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<DeleteCityCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var dialogs = new Mock<IDialogService>();
        dialogs.SetupSequence(service => service.ShowMessageBoxAsync(
                It.IsAny<string>(), It.IsAny<string>(), "حذف", null, "إلغاء", null))
            .ReturnsAsync((bool?)null)
            .ReturnsAsync((bool?)true);

        await using var fixture = new HtmlFixture(executor.Object, dialogs.Object);
        var index = await fixture.Render<CityIndex>();
        var city = new CityListItem(1, "Cairo");

        await fixture.Invoke(index, "ConfirmDeleteAsync", city);

        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<DeleteCityCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        await fixture.Invoke(index, "ConfirmDeleteAsync", city);

        executor.Verify(service => service.ExecuteAsync(
            It.Is<DeleteCityCommand>(command => command.Id == 1), It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllCitiesQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void City_requests_have_correct_permissions_and_only_one_page_route()
    {
        var requests = typeof(GetAllCitiesQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == "OrderHub.Application.Features.Setup.Cities" &&
                (type.Name.EndsWith("Query") || type.Name.EndsWith("Command")));

        foreach (var request in requests)
        {
            var permission = request.GetCustomAttribute<RequirePermissionAttribute>();

            Assert.NotNull(permission);
            Assert.Equal(PermissionFeature.Cities, permission.Feature);
            Assert.Equal(
                request.Name.EndsWith("Query") ? PermissionLevel.Read : PermissionLevel.Write,
                permission.RequiredLevel);
        }

        var route = Assert.Single(typeof(CityIndex).GetCustomAttributes<RouteAttribute>());

        Assert.Equal("/cities", route.Template);
        Assert.Empty(typeof(CityEditorDialog).GetCustomAttributes<RouteAttribute>());
        Assert.Equal(
            new PermissionAuthorizeAttribute(PermissionFeature.Cities, PermissionLevel.Read).Policy,
            typeof(CityIndex).GetCustomAttribute<PermissionAuthorizeAttribute>().Policy);
    }

    private static Mock<IRequestExecutor> ListExecutor(bool canWrite, IEnumerable<CityListItem> cities)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetAllCitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cities);
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<CheckPermissionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(canWrite);

        return executor;
    }

    private static T GetField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private sealed class HtmlFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly HtmlRenderer _renderer;
        private Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent _root;

        public Mock<IMudDialogInstance> DialogInstance { get; } = new();

        public string NavigationUri =>
            _services.GetRequiredService<NavigationManager>().Uri;

        public HtmlFixture(IRequestExecutor executor, IDialogService dialogs = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMudServices();
            services.AddSingleton<IJSRuntime>(new Mock<IJSRuntime>().Object);
            services.AddSingleton<NavigationManager>(new TestNavigation());
            services.AddSingleton(executor);
            services.AddSingleton<PermissionAccess>();

            if (dialogs is not null)
            {
                services.AddSingleton(dialogs);
            }

            _services = services.BuildServiceProvider();
            _renderer = new HtmlRenderer(_services, _services.GetRequiredService<ILoggerFactory>());
        }

        public async Task<T> Render<T>(Dictionary<string, object> parameters = null) where T : IComponent
        {
            T captured = default;
            RenderFragment fragment = builder =>
            {
                builder.OpenComponent<T>(0);
                builder.AddMultipleAttributes(1, parameters ?? []);
                builder.AddComponentReferenceCapture(2, component => captured = (T)component);
                builder.CloseComponent();
            };

            await _renderer.Dispatcher.InvokeAsync(async () =>
            {
                if (typeof(T) == typeof(CityEditorDialog))
                {
                    _root = await _renderer.RenderComponentAsync<MudDialogContainer>(
                        ParameterView.FromDictionary(new Dictionary<string, object>
                        {
                            [nameof(MudDialogContainer.Id)] = Guid.NewGuid(),
                            [nameof(MudDialogContainer.Options)] = new DialogOptions(),
                            [nameof(MudDialogContainer.Content)] = fragment
                        }));

                    typeof(CityEditorDialog)
                        .GetProperty("MudDialog", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(captured, DialogInstance.Object);

                    return;
                }

                _root = await _renderer.RenderComponentAsync<CascadingValue<IMudDialogInstance>>(
                    ParameterView.FromDictionary(new Dictionary<string, object>
                    {
                        [nameof(CascadingValue<IMudDialogInstance>.Value)] = DialogInstance.Object,
                        [nameof(CascadingValue<IMudDialogInstance>.IsFixed)] = true,
                        [nameof(CascadingValue<IMudDialogInstance>.ChildContent)] = fragment
                    }));
            });

            return captured;
        }

        public Task<string> Markup() => _renderer.Dispatcher.InvokeAsync(() =>
            WebUtility.HtmlDecode(_root.ToHtmlString()));

        public Task Invoke(object component, string method, params object[] arguments) =>
            _renderer.Dispatcher.InvokeAsync(async () =>
            {
                var result = component.GetType()
                    .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(component, arguments);

                if (result is Task task)
                {
                    await task;
                }

                await Refresh(component);
            });

        public Task SetField(object component, string field, object value) =>
            _renderer.Dispatcher.InvokeAsync(async () =>
            {
                component.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(component, value);

                await Refresh(component);
            });

        public Task Refresh(object component) => _renderer.Dispatcher.InvokeAsync(() =>
            typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(component, null));

        public async ValueTask DisposeAsync()
        {
            await _renderer.DisposeAsync();
            await _services.DisposeAsync();
        }
    }

    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation()
        {
            Initialize("http://localhost/", "http://localhost/cities");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }
}
