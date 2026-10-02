using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;
using static OrderHub.Web.Components.Pages.Features.Orders.Editor.Products.AddProductDialog;

namespace OrderHub.Tests.Web;

public class ProductAdditionRecoveryTests
{
    private static readonly ProductLookupItem Lookup = new(7, "Product", 12m, "P7", "Category", []);

    [Fact]
    public async Task Failed_add_preserves_values_and_retry_resets_only_after_success()
    {
        var dialog = new AddProductDialog();
        var instance = new Mock<IMudDialogInstance>();
        instance.SetupGet(x => x.Options).Returns(new DialogOptions());
        instance.Setup(x => x.SetOptionsAsync(It.IsAny<DialogOptions>())).Returns(Task.CompletedTask);
        Inject(dialog, "MudDialog", instance.Object);
        SetField(dialog, "_selectedProduct", Lookup);
        SetField(dialog, "_unitPrice", 15m);
        SetField(dialog, "_quantity", 3);
        SetField(dialog, "_vatRate", 10m);
        var calls = 0;
        dialog.ProductAdded = result =>
        {
            Assert.Equal(Lookup, result.Product);
            Assert.Equal(15m, result.UnitPrice);
            Assert.Equal(3, result.Quantity);
            Assert.Equal(10m, result.VatRate);
            return Task.FromResult(++calls == 1 ? Result.Failure("Retry") : Result.Success());
        };

        await InvokeTask(dialog, "Add");
        Assert.Same(Lookup, Field(dialog, "_selectedProduct"));
        Assert.Equal(15m, Field(dialog, "_unitPrice"));
        Assert.Equal(3, Field(dialog, "_quantity"));
        Assert.Equal(10m, Field(dialog, "_vatRate"));
        Assert.Equal("Retry", Field(dialog, "_addError"));
        Assert.False((bool)Field(dialog, "_isAdding"));

        await InvokeTask(dialog, "Add");
        Assert.Null(Field(dialog, "_selectedProduct"));
        Assert.Null(Field(dialog, "_addError"));
        Assert.Equal(2, calls);
        instance.Verify(x => x.Cancel(), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_product_or_read_failure_returns_failure_without_mutation(bool throws)
    {
        var executor = new Mock<IRequestExecutor>();
        var setup = executor.Setup(x => x.ExecuteAsync(
            It.IsAny<GetOrderItemEditor.Query>(), It.IsAny<CancellationToken>()));
        if (throws)
            setup.ThrowsAsync(new TimeoutException("Database timeout"));
        else
            setup.ReturnsAsync((GetOrderItemEditor.OrderItemEditorDto)null);

        var step = new ProductsStep { Items = [] };
        var notifications = 0;
        step.ItemsChanged = EventCallback.Factory.Create(new object(), () => notifications++);
        Inject(step, "RequestExecutor", executor.Object);
        Inject(step, "Logger", NullLogger<ProductsStep>.Instance);

        var result = await InvokeResult(step, "OnProductAddedAsync", new AddProductResult(Lookup, 15, 3, 10));
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.ErrorMessage);
        Assert.Empty(step.Items);
        Assert.Equal(0, notifications);
    }

    [Fact]
    public async Task Successful_hydration_adds_complete_item_and_returns_success()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderItemEditor.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetOrderItemEditor.OrderItemEditorDto(7, 12, "Product", "Category", [], []));
        var step = new HeadlessProductsStep { Items = [] };
        var notifications = 0;
        step.ItemsChanged = EventCallback.Factory.Create(new object(), () => notifications++);
        Inject(step, "RequestExecutor", executor.Object);
        Inject(step, "Logger", NullLogger<ProductsStep>.Instance);
        using var renderer = new TestRenderer();
        renderer.Attach(step);

        var result = await InvokeResult(step, "OnProductAddedAsync", new AddProductResult(Lookup, 15, 3, 10));

        Assert.True(result.IsSuccess);
        var item = Assert.Single(step.Items);
        Assert.Equal(7, item.ProductId);
        Assert.Equal(15m, item.UnitPrice);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(10m, item.VatRate);
        Assert.Equal(1, notifications);
        await InvokeTask(step, "RemoveItem", item);
        Assert.Equal(2, notifications);
        Assert.Empty(step.Items);
        await InvokeTask(step, "RemoveItem", item);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public async Task Search_failure_is_recoverable_and_retry_can_succeed()
    {
        var store = new Mock<IProductStore>();
        store.SetupSequence(x => x.GetProductsByName("Product", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database timeout"))
            .ReturnsAsync(new[] { Lookup });
        using var dialog = new AddProductDialog();
        Inject(dialog, "ProductStore", store.Object);
        Inject(dialog, "Logger", NullLogger<AddProductDialog>.Instance);

        await InvokeTask(dialog, "TextChanged", "Product");
        Assert.NotNull(Field(dialog, "_searchError"));
        Assert.Empty((IReadOnlyList<ProductLookupItem>)Field(dialog, "_products"));
        await InvokeTask(dialog, "TextChanged", "Product");
        Assert.Null(Field(dialog, "_searchError"));
        Assert.Single((IReadOnlyList<ProductLookupItem>)Field(dialog, "_products"));
    }

    private static PropertyInfo Property(object target, string name) =>
        typeof(AddProductDialog).IsInstanceOfType(target)
            ? typeof(AddProductDialog).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance)
            : typeof(ProductsStep).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static void Inject(object target, string name, object value) => Property(target, name).SetValue(target, value);
    private static object Field(object target, string name) => typeof(AddProductDialog)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void SetField(object target, string name, object value) => typeof(AddProductDialog)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static MethodInfo Method(object target, string name) =>
        (target is AddProductDialog ? typeof(AddProductDialog) : typeof(ProductsStep))
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static Task InvokeTask(object target, string name, params object[] args) => (Task)Method(target, name).Invoke(target, args);
    private static Task<Result> InvokeResult(object target, string name, params object[] args) => (Task<Result>)Method(target, name).Invoke(target, args);

    private sealed class HeadlessProductsStep : ProductsStep
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    // Attach a render handle to exercise the component's successful mutation/rerender path.
    private sealed class TestRenderer : Renderer
    {
        public TestRenderer() : base(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance) { }
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public void Attach(IComponent component) => AssignRootComponentId(component);
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception) => throw exception;
    }
}
