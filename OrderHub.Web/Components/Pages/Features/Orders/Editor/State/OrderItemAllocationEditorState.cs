using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

public sealed class OrderItemAllocationEditorState
{
    public AllocationSourceKind SourceKind { get; init; }

    public int Qty { get; set; }

    public int? WarehouseId { get; init; }

    public string? WarehouseName { get; init; }

    public int? SupplierId { get; init; }

    public string? SupplierName { get; init; }

    public OrderItemAllocationEditorState Clone() => new()
    {
        SourceKind = SourceKind,
        Qty = Qty,
        WarehouseId = WarehouseId,
        WarehouseName = WarehouseName,
        SupplierId = SupplierId,
        SupplierName = SupplierName
    };
}
