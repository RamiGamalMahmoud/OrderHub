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
using MediatR;
using MudBlazor;
using MudBlazor.Services;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Application.Features.Setup.Properties.Delete;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;
using OrderHub.Web.Components.Pages.Features.Properties.Editor;
using OrderHub.Web.Infrastructure.Authorization;
using OrderHub.Web.Services;
using PropertyIndex = OrderHub.Web.Components.Pages.Features.Properties.Index;

namespace OrderHub.Tests.Web;

public class ProductPropertiesWebTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Index_shows_counts_and_only_authorized_mutation_actions(bool canWrite)
    {
        var executor = ListExecutor(canWrite);
        await using var fixture = new HtmlFixture(executor.Object);
        await fixture.Render<PropertyIndex>();

        string html = await fixture.Markup();

        Assert.Contains("Color", html);
        Assert.Contains("3 خيارات", html);
        Assert.Equal(canWrite, html.Contains("إضافة خاصية"));
        Assert.Equal(canWrite, html.Contains("تعديل الخاصية"));
        Assert.Equal(canWrite, html.Contains("حذف الخاصية"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(4)]
    public async Task One_dialog_handles_initial_values_cancel_and_successful_save(int? propertyId)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetPropertyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDetails(4, "Color", PropertyType.List, "Description", [new(7, "Red")]));
        executor.Setup(service => service.ExecuteAsync(It.IsAny<CreatePropertyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        executor.Setup(service => service.ExecuteAsync(It.IsAny<UpdatePropertyCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<PropertyEditorDialog>(new() { [nameof(PropertyEditorDialog.PropertyId)] = propertyId });
        var state = GetField<PropertyEditorState>(dialog, "_state");

        Assert.Equal(propertyId.HasValue ? "Color" : "", state.Name);

        if (propertyId.HasValue)
        {
            Assert.Equal(7, Assert.Single(state.Options).Id);
            Assert.Contains("Red", await fixture.Markup());
        }

        state.Name = "Edited";
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        fixture.DialogInstance.Verify(instance => instance.Close(
            It.Is<DialogResult>(result => !result.Canceled && Equals(result.Data, true))), Times.Once);

        if (propertyId.HasValue)
        {
            executor.Verify(service => service.ExecuteAsync(
                It.Is<UpdatePropertyCommand>(command => command.Input.Options.Single().Id == 7),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        await fixture.Invoke(dialog, "Cancel");
        fixture.DialogInstance.Verify(instance => instance.Cancel(), Times.Once);
    }

    [Fact]
    public async Task Dynamic_rows_validate_duplicates_and_business_failure_keeps_the_dialog_open()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(It.IsAny<CreatePropertyCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("توجد خاصية بهذا الاسم بالفعل."));

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<PropertyEditorDialog>();
        var state = GetField<PropertyEditorState>(dialog, "_state");
        state.Name = "Color";
        await fixture.Invoke(dialog, "ChangeType", PropertyType.List);
        Assert.Single(state.Options);
        state.Options[0].Value = "Red";
        await fixture.Invoke(dialog, "AddOption");
        state.Options[1].Value = " red ";
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        Assert.Contains("لا يمكن تكرار قيم الخيارات.", await fixture.Markup());
        executor.Verify(service => service.ExecuteAsync(It.IsAny<CreatePropertyCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        await fixture.Invoke(dialog, "RemoveOption", state.Options[1]);
        await fixture.Invoke(dialog, "SaveAsync");

        Assert.Contains("توجد خاصية بهذا الاسم بالفعل.", await fixture.Markup());
        fixture.DialogInstance.Verify(instance => instance.Close(It.IsAny<DialogResult>()), Times.Never);
    }

    [Fact]
    public async Task Index_refreshes_after_dialog_success_and_application_denial_uses_shared_flow()
    {
        var executor = ListExecutor(true);
        var dialogs = new Mock<IDialogService>();
        var saved = new Mock<IDialogReference>();
        saved.SetupGet(reference => reference.Result).Returns(Task.FromResult(DialogResult.Ok(true)));
        dialogs.Setup(service => service.ShowAsync<PropertyEditorDialog>(
                It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(saved.Object);

        await using var fixture = new HtmlFixture(executor.Object, dialogs.Object);
        var index = await fixture.Render<PropertyIndex>();
        await fixture.Invoke(index, "OpenCreateDialogAsync");

        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetPropertiesQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Dialog_denial_and_missing_property_are_handled_without_writes()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetPropertyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDetails)null);
        executor.Setup(service => service.ExecuteAsync(It.IsAny<CreatePropertyCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthorizationDeniedException());

        await using (var missing = new HtmlFixture(executor.Object))
        {
            await missing.Render<PropertyEditorDialog>(new() { [nameof(PropertyEditorDialog.PropertyId)] = 4 });
            Assert.Contains("غير موجودة", await missing.Markup());
        }

        await using var fixture = new HtmlFixture(executor.Object);
        var dialog = await fixture.Render<PropertyEditorDialog>();
        GetField<PropertyEditorState>(dialog, "_state").Name = "Text property";
        await fixture.Refresh(dialog);
        await fixture.Invoke(dialog, "SaveAsync");

        fixture.DialogInstance.Verify(instance => instance.Cancel(), Times.Once);
        Assert.EndsWith("/access-denied", fixture.NavigationUri);
    }

    [Fact]
    public async Task Application_pipeline_blocks_all_property_mutations_for_read_only_actor()
    {
        var authorization = new Mock<IAuthorizationService>();
        authorization.Setup(service => service.IsAllowedAsync(
                PermissionFeature.Properties, PermissionLevel.Write, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        bool changed = false;

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            new AuthorizationBehavior<CreatePropertyCommand, int>(authorization.Object).Handle(
                new("Denied", PropertyType.Text, "", []),
                _ =>
                {
                    changed = true;
                    return Task.FromResult(1);
                }, default));
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            new AuthorizationBehavior<UpdatePropertyCommand, Unit>(authorization.Object).Handle(
                new(new(1, "Denied", "", PropertyType.Text, [])),
                _ =>
                {
                    changed = true;
                    return Task.FromResult(Unit.Value);
                }, default));
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            new AuthorizationBehavior<DeletePropertyCommand, OrderHub.Domain.Common.Result>(authorization.Object).Handle(
                new(1),
                _ =>
                {
                    changed = true;
                    return Task.FromResult(OrderHub.Domain.Common.Result.Success());
                }, default));

        Assert.False(changed);
    }

    [Fact]
    public async Task Empty_index_and_cancel_do_not_expose_or_execute_mutations()
    {
        var executor = ListExecutor(false);
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetPropertiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PropertyListItem>());

        await using (var fixture = new HtmlFixture(executor.Object))
        {
            await fixture.Render<PropertyIndex>();
            Assert.Contains("لم تتم إضافة خصائص بعد.", await fixture.Markup());
            Assert.DoesNotContain("إضافة خاصية", await fixture.Markup());
        }

        await using var cancelFixture = new HtmlFixture(executor.Object);
        var dialog = await cancelFixture.Render<PropertyEditorDialog>();
        await cancelFixture.Invoke(dialog, "Cancel");

        executor.Verify(service => service.ExecuteAsync(It.IsAny<CreatePropertyCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        cancelFixture.DialogInstance.Verify(instance => instance.Cancel(), Times.Once);
    }

    [Fact]
    public async Task Deletion_requires_confirmation_and_refreshes_after_success()
    {
        var executor = ListExecutor(true);
        executor.Setup(service => service.ExecuteAsync(It.IsAny<DeletePropertyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderHub.Domain.Common.Result.Success());
        var dialogs = new Mock<IDialogService>();
        dialogs.SetupSequence(service => service.ShowMessageBoxAsync(
                It.IsAny<string>(), It.IsAny<string>(), "حذف", null, "إلغاء", null))
            .ReturnsAsync((bool?)null)
            .ReturnsAsync((bool?)true);

        await using var fixture = new HtmlFixture(executor.Object, dialogs.Object);
        var index = await fixture.Render<PropertyIndex>();
        var property = new PropertyListItem(1, "Color", "", PropertyType.List, 3);
        await fixture.Invoke(index, "ConfirmDeleteAsync", property);

        executor.Verify(service => service.ExecuteAsync(It.IsAny<DeletePropertyCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        await fixture.Invoke(index, "ConfirmDeleteAsync", property);

        executor.Verify(service => service.ExecuteAsync(It.IsAny<DeletePropertyCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetPropertiesQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void Permissions_and_single_route_match_existing_architecture()
    {
        var requests = new[] { typeof(GetPropertiesQuery), typeof(GetPropertyQuery),
            typeof(CreatePropertyCommand), typeof(UpdatePropertyCommand), typeof(DeletePropertyCommand) };

        foreach (var request in requests)
        {
            var permission = request.GetCustomAttribute<RequirePermissionAttribute>();
            Assert.Equal(PermissionFeature.Properties, permission.Feature);
            Assert.Equal(request.Name.EndsWith("Query") ? PermissionLevel.Read : PermissionLevel.Write, permission.RequiredLevel);
        }

        Assert.Equal("/properties", Assert.Single(typeof(PropertyIndex).GetCustomAttributes<RouteAttribute>()).Template);
        Assert.Empty(typeof(PropertyEditorDialog).GetCustomAttributes<RouteAttribute>());
        Assert.Equal(new PermissionAuthorizeAttribute(PermissionFeature.Properties, PermissionLevel.Read).Policy,
            typeof(PropertyIndex).GetCustomAttribute<PermissionAuthorizeAttribute>().Policy);
    }

    private static Mock<IRequestExecutor> ListExecutor(bool canWrite)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetPropertiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PropertyListItem(1, "Color", "", PropertyType.List, 3) });
        executor.Setup(service => service.ExecuteAsync(It.IsAny<CheckPermissionQuery>(), It.IsAny<CancellationToken>()))
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
                if (typeof(T) == typeof(PropertyEditorDialog))
                {
                    _root = await _renderer.RenderComponentAsync<MudDialogContainer>(
                        ParameterView.FromDictionary(new Dictionary<string, object>
                        {
                            [nameof(MudDialogContainer.Id)] = Guid.NewGuid(),
                            [nameof(MudDialogContainer.Options)] = new DialogOptions(),
                            [nameof(MudDialogContainer.Content)] = fragment
                        }));

                    typeof(PropertyEditorDialog)
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
            Initialize("http://localhost/", "http://localhost/properties");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }

}

