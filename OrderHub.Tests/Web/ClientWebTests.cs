using System;
using System.Collections.Generic;
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
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Clients.Editor;
using OrderHub.Web.Infrastructure.Authorization;
using OrderHub.Web.Services;
using FeatureIndex = OrderHub.Web.Components.Pages.Features.Clients.Index;

namespace OrderHub.Tests.Web;

public class ClientWebTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Index_respects_write_access_and_search_is_local(bool canWrite)
    {
        var executor = ListExecutor(canWrite, [new ClientListItem(1, "Cairo", "Cairo - Street", "+201234567", null), new ClientListItem(2, "Giza", "Giza - Street", "+201234567", null)]);
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();

        string html = await fixture.Markup();

        Assert.Equal(canWrite, html.Contains("إضافة عميل"));
        Assert.Equal(canWrite, html.Contains("تعديل العميل"));
        Assert.Equal(canWrite, html.Contains("حذف العميل"));

        await fixture.SetField(index, "_searchTerm", " cairo ");

        html = await fixture.Markup();

        Assert.Contains("Cairo", html);
        Assert.DoesNotContain("Giza", html);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllClientsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_index_explains_state(bool canWrite)
    {
        await using var fixture = new HtmlFixture(ListExecutor(canWrite, []).Object);
        await fixture.Render<FeatureIndex>();

        Assert.Contains("لم تتم إضافة عملاء بعد.", await fixture.Markup());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(4)]
    public async Task Editor_initializes_own_mode_saves_own_command_and_cancels(int? id)
    {
        var executor = EditorExecutor();
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<UpdateClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<ClientEditorDialog>(new()
        {
            [nameof(ClientEditorDialog.ClientId)] = id
        });

        var state = GetField<ClientEditorState>(dialog, "_state");
        Assert.Equal(id.HasValue ? "Cairo" : "", state.Name);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetClientEditQuery>(), It.IsAny<CancellationToken>()),
            id.HasValue ? Times.Once() : Times.Never());

        Fill(state);
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()),
            id.HasValue ? Times.Never() : Times.Once());
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<UpdateClientCommand>(), It.IsAny<CancellationToken>()),
            id.HasValue ? Times.Once() : Times.Never());
        fixture.DialogInstance.Verify(instance => instance.Close(
            It.Is<DialogResult>(result => !result.Canceled && Equals(result.Data, true))), Times.Once);

        await fixture.Invoke(dialog, "Cancel");
        fixture.DialogInstance.Verify(instance => instance.Cancel(), Times.Once);
    }

    [Fact]
    public async Task Invalid_input_and_business_failure_stay_open_and_allow_retry()
    {
        var executor = EditorExecutor();
        executor.SetupSequence(service => service.ExecuteAsync(
                It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("تعذر الحفظ لوجود ارتباط."))
            .ReturnsAsync(Result.Success());

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<ClientEditorDialog>();

        await fixture.Invoke(dialog, "SaveAsync");

        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        Fill(GetField<ClientEditorState>(dialog, "_state"));
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        Assert.Contains("تعذر الحفظ لوجود ارتباط.", await fixture.Markup());
        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Never);

        await fixture.Invoke(dialog, "SaveAsync");

        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_target_or_no_cities_explains_failure_and_blocks_save(bool missing)
    {
        var executor = EditorExecutor();
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetClientEditQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientInput)null);
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetClientEditorCitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CityInfo>());

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<ClientEditorDialog>(new()
        {
            [nameof(ClientEditorDialog.ClientId)] = missing ? 4 : null
        });

        Assert.Contains(
            missing ? "تعذر العثور على العميل المطلوب." : "لا توجد مدن متاحة.",
            await fixture.Markup());

        await fixture.Invoke(dialog, "SaveAsync");

        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<UpdateClientCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Index_refreshes_only_after_successful_editor_result()
    {
        var executor = ListExecutor(true, [new ClientListItem(1, "Cairo", "Cairo - Street", "+201234567", null)]);
        var dialogs = new Mock<IDialogService>();
        var canceled = new Mock<IDialogReference>();
        canceled.SetupGet(reference => reference.Result).Returns(Task.FromResult(DialogResult.Cancel()));
        var saved = new Mock<IDialogReference>();
        saved.SetupGet(reference => reference.Result).Returns(Task.FromResult(DialogResult.Ok(true)));
        dialogs.SetupSequence(service => service.ShowAsync<ClientEditorDialog>(
                It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(canceled.Object)
            .ReturnsAsync(saved.Object);

        await using var fixture = new HtmlFixture(executor.Object, dialogs.Object);
        var index = await fixture.Render<FeatureIndex>();

        await fixture.Invoke(index, "OpenCreateDialogAsync");
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllClientsQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        await fixture.Invoke(index, "OpenEditDialogAsync", new ClientListItem(1, "Cairo", "Cairo - Street", "+201234567", null));
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllClientsQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Delete_requires_confirmation_and_refreshes_only_after_success()
    {
        var executor = ListExecutor(true, [new ClientListItem(1, "Cairo", "Cairo - Street", "+201234567", null)]);
        executor.SetupSequence(service => service.ExecuteAsync(
                It.IsAny<DeleteClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("السجل مرتبط بطلب."))
            .ReturnsAsync(Result.Success());
        var dialogs = new Mock<IDialogService>();
        dialogs.SetupSequence(service => service.ShowMessageBoxAsync(
                It.IsAny<string>(), It.IsAny<string>(), "حذف", null, "إلغاء", null))
            .ReturnsAsync((bool?)null)
            .ReturnsAsync((bool?)true)
            .ReturnsAsync((bool?)true);

        await using var fixture = new HtmlFixture(executor.Object, dialogs.Object);
        var index = await fixture.Render<FeatureIndex>();
        var row = new ClientListItem(1, "Cairo", "Cairo - Street", "+201234567", null);

        await fixture.Invoke(index, "ConfirmDeleteAsync", row);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<DeleteClientCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        await fixture.Invoke(index, "ConfirmDeleteAsync", row);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllClientsQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        await fixture.Invoke(index, "ConfirmDeleteAsync", row);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<GetAllClientsQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Client_search_includes_optional_location_and_phone()
    {
        var executor = ListExecutor(false,
        [
            new ClientListItem(1, "First", "City - Street", "+20 - 123456", "Near station"),
            new ClientListItem(2, "Second", "Other - Street", "+20 - 987654", null)
        ]);
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        await fixture.SetField(index, "_searchTerm", " station ");
        Assert.Contains("First", await fixture.Markup());
        Assert.DoesNotContain("Second", await fixture.Markup());
        await fixture.SetField(index, "_searchTerm", "987654");
        Assert.Contains("Second", await fixture.Markup());
        Assert.DoesNotContain("First", await fixture.Markup());
    }

    [Fact]
    public void Index_has_only_its_feature_route_and_read_policy()
    {
        Assert.Equal("/clients",
            Assert.Single(typeof(FeatureIndex).GetCustomAttributes<RouteAttribute>()).Template);
        Assert.Empty(typeof(ClientEditorDialog).GetCustomAttributes<RouteAttribute>());
        Assert.Equal(
            new PermissionAuthorizeAttribute(PermissionFeature.Clients, PermissionLevel.Read).Policy,
            typeof(FeatureIndex).GetCustomAttribute<PermissionAuthorizeAttribute>().Policy);
    }

    [Fact]
    public async Task Unexpected_save_failure_is_recoverable_and_denial_navigates_to_access_denied()
    {
        var executor = EditorExecutor();
        executor.SetupSequence(service => service.ExecuteAsync(
                It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException())
            .ThrowsAsync(new AuthorizationDeniedException());

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<ClientEditorDialog>();
        Fill(GetField<ClientEditorState>(dialog, "_state"));
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        Assert.Contains("تعذر حفظ العميل", await fixture.Markup());
        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Never);

        await fixture.Invoke(dialog, "SaveAsync");

        fixture.DialogInstance.Verify(instance => instance.Cancel(), Times.Once);
        Assert.EndsWith("/access-denied", fixture.NavigationUri);
    }

    [Fact]
    public async Task Saving_blocks_repeated_submit_and_cancel_until_persistence_completes()
    {
        var executor = EditorExecutor();
        var completion = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()))
            .Returns(completion.Task);

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<ClientEditorDialog>();
        Fill(GetField<ClientEditorState>(dialog, "_state"));
        await fixture.Refresh(dialog);
        Task first = fixture.Invoke(dialog, "SaveAsync");

        await fixture.Invoke(dialog, "SaveAsync");
        await fixture.Invoke(dialog, "Cancel");

        fixture.DialogInstance.Verify(instance => instance.Cancel(), Times.Never);
        executor.Verify(service => service.ExecuteAsync(
            It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()), Times.Once);

        completion.SetResult(Result.Success());
        await first;

        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Once);
    }

    private static void Fill(ClientEditorState state)
    {
        state.Name = "Alexandria";
        state.CityId = 1;
        state.Street = "Street";
        state.PhoneNumber = "1234567";
        state.CountryCode = "+20";
        state.Location = "Location";
    }

    private static Mock<IRequestExecutor> EditorExecutor()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetClientEditQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientInput("Cairo", "Street", 1, "1234567", "+20", null));
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetClientEditorCitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new CityInfo(1, "Cairo") });

        return executor;
    }

    private static Mock<IRequestExecutor> ListExecutor(
        bool canWrite,
        IEnumerable<ClientListItem> items)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(
                It.IsAny<GetAllClientsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
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
                if (typeof(T) == typeof(ClientEditorDialog))
                {
                    _root = await _renderer.RenderComponentAsync<MudDialogContainer>(
                        ParameterView.FromDictionary(new Dictionary<string, object>
                        {
                            [nameof(MudDialogContainer.Id)] = Guid.NewGuid(),
                            [nameof(MudDialogContainer.Options)] = new DialogOptions(),
                            [nameof(MudDialogContainer.Content)] = fragment
                        }));

                    typeof(ClientEditorDialog)
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
            Initialize("http://localhost/", "http://localhost/clients");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }
}
