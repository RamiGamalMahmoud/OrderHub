using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products.Models;

public sealed class ProductEditorProperty
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public bool IsRequired { get; init; }

    public PropertyType Type { get; init; }

    public IReadOnlyList<ProductEditorPropertyOption> Options { get; init; } = [];
}