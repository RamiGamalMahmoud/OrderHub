namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products.Models;

public sealed class ProductEditorData
{
    public int ProductId { get; init; }

    public required string ProductName { get; init; }

    public required string CategoryName { get; init; }

    public IReadOnlyList<ProductEditorSupplier> Suppliers { get; init; } = [];

    public IReadOnlyList<ProductEditorProperty> Properties { get; init; } = [];
}