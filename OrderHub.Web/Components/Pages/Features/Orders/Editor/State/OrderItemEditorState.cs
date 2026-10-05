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

    public int? PersistedSupplierId { get; init; }

    public string? SupplierName { get; init; }

    public string? MetadataRestorationError { get; init; }

    public IReadOnlyList<OrderItemSupplierEditorState> Suppliers { get; init; } = [];

    public List<OrderItemPropertyEditorState> Properties { get; init; } = [];

    public OrderItemEditorState CreateWorkingCopy() => new()
    {
        ProductId = ProductId,
        ProductName = ProductName,
        CategoryName = CategoryName,
        UnitPrice = UnitPrice,
        Quantity = Quantity,
        VatRate = VatRate,
        SupplierId = SupplierId,
        PersistedSupplierId = PersistedSupplierId,
        SupplierName = SupplierName,
        MetadataRestorationError = MetadataRestorationError,
        Suppliers = Suppliers.ToArray(),
        Properties = Properties.Select(property => new OrderItemPropertyEditorState
        {
            PropertyId = property.PropertyId,
            Name = property.Name,
            HasMetadata = property.HasMetadata,
            IsRequired = property.IsRequired,
            Type = property.Type,
            Value = property.Value,
            Options = property.Options.ToArray()
        }).ToList()
    };
}
