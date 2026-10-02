using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class OrderItemPropertyEditorState
{
    public int PropertyId { get; init; }

    public required string Name { get; init; }

    public bool IsRequired { get; init; }

    public PropertyType Type { get; init; }

    public string? Value { get; set; }

    public IReadOnlyList<OrderItemPropertyOptionEditorState> Options { get; init; } = [];
}