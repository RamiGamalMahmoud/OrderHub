using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;
using System;
using System.Reflection;
using System.Threading.Tasks;
using OrderHub.Domain.Common;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

namespace OrderHub.Tests.Web;

public class ProductWorkingCopyTests
{
    [Fact]
    public async Task Commit_notifies_once_for_a_change_and_rejects_removed_or_stale_targets()
    {
        var original = new OrderItemEditorState { ProductName = "P", CategoryName = "C", UnitPrice = 5 };
        var notifications = 0;
        using var step = new ProductsStep
        {
            Items = [original], SessionId = Guid.NewGuid(),
            ItemsChanged = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => notifications++)
        };
        var method = typeof(ProductsStep).GetMethod("CommitItemAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Task<Result> Commit(Guid session, OrderItemEditorState copy) => (Task<Result>)method.Invoke(step, new object[] { original, session, copy });
        var working = original.CreateWorkingCopy();
        Assert.True((await Commit(step.SessionId, working)).IsSuccess);
        Assert.Equal(0, notifications);
        working.UnitPrice = 10;
        Assert.False((await Commit(Guid.NewGuid(), working)).IsSuccess);
        Assert.Equal(5m, original.UnitPrice);
        Assert.True((await Commit(step.SessionId, working)).IsSuccess);
        Assert.Equal(1, notifications);
        Assert.True((await Commit(step.SessionId, working)).IsSuccess);
        Assert.Equal(1, notifications);
        step.Items.Clear();
        working.UnitPrice = 20;
        Assert.False((await Commit(step.SessionId, working)).IsSuccess);
        Assert.Equal(10m, original.UnitPrice);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task Drawer_cancel_discards_nested_changes_and_save_closes_only_after_success()
    {
        var original = new OrderItemEditorState
        {
            ProductName = "P", CategoryName = "C", UnitPrice = 5,
            Properties = [new() { PropertyId = 1, Name = "Field", Value = "Original" }]
        };
        var closed = 0;
        var commits = 0;
        var outcome = Result.Failure("Retry");
        var drawer = new ProductEditorDrawer
        {
            Item = original, IsOpen = true, SessionId = Guid.NewGuid(),
            IsOpenChanged = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<bool>(new object(), open => { if (!open) closed++; }),
            Commit = (_, _, _) => { commits++; return Task.FromResult(outcome); }
        };
        var type = typeof(ProductEditorDrawer);
        void Open() => type.GetMethod("OnParametersSet", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(drawer, null);
        Task Invoke(string method) => (Task)type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(drawer, null);
        OrderItemEditorState Copy() => (OrderItemEditorState)type.GetField("_workingCopy", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(drawer);
        Open();
        Copy().Properties[0].Value = "Cancelled";
        await Invoke("Cancel");
        Assert.Equal("Original", original.Properties[0].Value);
        Assert.Equal(0, commits);
        Assert.Equal(1, closed);
        drawer.IsOpen = false;
        Open();
        drawer.IsOpen = true;
        Open();
        Copy().Properties[0].Value = "Pending";
        await Invoke("SaveAsync");
        Assert.Equal(1, commits);
        Assert.Equal(1, closed);
        Assert.Equal("Pending", Copy().Properties[0].Value);
        outcome = Result.Success();
        await Invoke("SaveAsync");
        Assert.Equal(2, commits);
        Assert.Equal(2, closed);
        Assert.Null(Copy());
    }

    [Theory]
    [InlineData(OrderHub.Domain.Enums.PropertyType.Text, true, null, false)]
    [InlineData(OrderHub.Domain.Enums.PropertyType.Text, true, "   ", false)]
    [InlineData(OrderHub.Domain.Enums.PropertyType.Text, false, null, true)]
    [InlineData(OrderHub.Domain.Enums.PropertyType.Boolean, true, "false", true)]
    [InlineData(OrderHub.Domain.Enums.PropertyType.Boolean, true, null, false)]
    [InlineData(OrderHub.Domain.Enums.PropertyType.Boolean, false, "invalid", false)]
    [InlineData(OrderHub.Domain.Enums.PropertyType.List, true, "Available", true)]
    [InlineData(OrderHub.Domain.Enums.PropertyType.List, false, "Removed", false)]
    public async Task Property_validation_uses_original_definitions_before_any_mutation(
        OrderHub.Domain.Enums.PropertyType type, bool required, string value, bool valid)
    {
        var original = new OrderItemEditorState
        {
            ProductName = "P", CategoryName = "C", UnitPrice = 5,
            Properties = [new() { PropertyId = 1, Name = "Field", Type = type, IsRequired = required, Options = [new(1, "Available")] }]
        };
        using var step = new ProductsStep { Items = [original], SessionId = Guid.NewGuid() };
        var working = original.CreateWorkingCopy();
        working.UnitPrice = 10;
        working.Properties[0].Value = value;
        var result = await (Task<Result>)typeof(ProductsStep).GetMethod("CommitItemAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(step, new object[] { original, step.SessionId, working });
        Assert.Equal(valid, result.IsSuccess);
        Assert.Equal(valid ? 10m : 5m, original.UnitPrice);
        Assert.Equal(valid ? value : null, original.Properties[0].Value);
    }

    [Fact]
    public async Task Supplier_save_targets_the_original_row_and_rejects_unavailable_supplier()
    {
        var original = new OrderItemEditorState { ProductId = 7, ProductName = "P", CategoryName = "C", UnitPrice = 5, Suppliers = [new(3, "S")] };
        var duplicate = original.CreateWorkingCopy();
        using var step = new ProductsStep { Items = [original, duplicate], SessionId = Guid.NewGuid() };
        var working = original.CreateWorkingCopy();
        working.SupplierId = 99;
        var commit = typeof(ProductsStep).GetMethod("CommitItemAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.False((await (Task<Result>)commit.Invoke(step, new object[] { original, step.SessionId, working })).IsSuccess);
        Assert.Null(original.SupplierId);
        working.SupplierId = 3;
        Assert.True((await (Task<Result>)commit.Invoke(step, new object[] { original, step.SessionId, working })).IsSuccess);
        Assert.Equal(3, original.SupplierId);
        Assert.Null(duplicate.SupplierId);
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(5, 0, 0)]
    [InlineData(5, 1, -1)]
    [InlineData(5, 1, 101)]
    public async Task Invalid_basic_values_cannot_commit(decimal price, int quantity, decimal vat)
    {
        var original = new OrderItemEditorState { ProductName = "P", CategoryName = "C", UnitPrice = 5, Quantity = 1 };
        using var step = new ProductsStep { Items = [original], SessionId = Guid.NewGuid() };
        var working = original.CreateWorkingCopy();
        working.UnitPrice = price;
        working.Quantity = quantity;
        working.VatRate = vat;
        var result = await (Task<Result>)typeof(ProductsStep)
            .GetMethod("CommitItemAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(step, new object[] { original, step.SessionId, working });
        Assert.False(result.IsSuccess);
        Assert.Equal(5m, original.UnitPrice);
        Assert.Equal(1, original.Quantity);
        Assert.Equal(0m, original.VatRate);
    }

    [Fact]
    public void Working_copy_changes_cannot_mutate_original_or_nested_collections()
    {
        var original = new OrderItemEditorState
        {
            ProductId = 7, ProductName = "Product", CategoryName = "Category", UnitPrice = 10,
            Quantity = 2, VatRate = 5, SupplierId = 3, Suppliers = [new(3, "Supplier")],
            Properties = [new() { PropertyId = 1, Name = "Property", Value = "Original", Options = [new(4, "Option")] }]
        };
        var copy = original.CreateWorkingCopy();
        copy.UnitPrice = 20;
        copy.Quantity = 4;
        copy.VatRate = 10;
        copy.SupplierId = null;
        copy.Properties[0].Value = "Edited";
        Assert.Equal(10m, original.UnitPrice);
        Assert.Equal(2, original.Quantity);
        Assert.Equal(5m, original.VatRate);
        Assert.Equal(3, original.SupplierId);
        Assert.Equal("Original", original.Properties[0].Value);
        Assert.NotSame(original.Properties, copy.Properties);
        Assert.NotSame(original.Properties[0], copy.Properties[0]);
        Assert.NotSame(original.Properties[0].Options, copy.Properties[0].Options);
        Assert.NotSame(original.Suppliers, copy.Suppliers);
    }
}
