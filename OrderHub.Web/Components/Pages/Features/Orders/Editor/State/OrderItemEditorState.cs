using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class OrderItemEditorState
{
    public int ProductId { get; init; }

    public int? ItemId { get; set; }

    public required string ProductName { get; init; }

    public required string CategoryName { get; init; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; } = 1;

    public decimal VatRate { get; set; }

    public int? SupplierId { get; set; }

    public int? PersistedSupplierId { get; init; }

    public string? SupplierName { get; init; }

    public string? MetadataRestorationError { get; init; }

    // قابلة للاستبدال حتى تتمكن أحداث إضافة مورد من تحديث القائمة.
    public IReadOnlyList<OrderItemSupplierEditorState> Suppliers { get; set; } = [];

    public List<OrderItemPropertyEditorState> Properties { get; init; } = [];

    // مصدر كل جزء من الكمية: مخزن أو مورد. تُملأ عند حفظ المنتج في المحرر.
    public List<OrderItemAllocationEditorState> Allocations { get; init; } = [];

    public int AllocatedQuantity => Allocations.Sum(allocation => allocation.Qty);

    public int WarehouseQuantity => Allocations
        .Where(allocation => allocation.SourceKind == AllocationSourceKind.Warehouse)
        .Sum(allocation => allocation.Qty);

    public int SupplierQuantity => Allocations
        .Where(allocation => allocation.SourceKind == AllocationSourceKind.Supplier)
        .Sum(allocation => allocation.Qty);

    public OrderItemEditorState CreateWorkingCopy() => new()
    {
        ProductId = ProductId,
        ItemId = ItemId,
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
        Allocations = Allocations.Select(allocation => allocation.Clone()).ToList(),
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
