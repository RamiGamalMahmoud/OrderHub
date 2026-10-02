namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class OrderItemEditorState
{
    public int ProductId { get; init; }

    public required string ProductName { get; init; }

    public required string CategoryName { get; init; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; } = 1;

    public decimal VatRate { get; set; }

    public int? SupplierId { get; set; }

    public IReadOnlyList<OrderItemSupplierEditorState> Suppliers { get; init; } = [];

    public List<OrderItemPropertyEditorState> Properties { get; init; } = [];
}