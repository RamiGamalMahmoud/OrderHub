using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Tests.Web;

public class ProductWorkingCopyTests
{
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
