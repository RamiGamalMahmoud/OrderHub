using OrderHub.Application.Features.Inventory.GetStockInContext;
using OrderHub.Application.Features.Inventory.UploadStockAttachment;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Web.Components.Pages.Features.Inventory.StockIn;

public sealed class StockInState
{
    public StockInState(StockInContext context)
    {
        WarehouseId = context.WarehouseId;
        WarehouseName = context.WarehouseName;
        IsOpeningAllowed = context.IsOpeningAllowed;
        CanReadProducts = context.CanReadProducts;
        CanSelectSupplier = context.CanSelectSupplier;
    }

    public int WarehouseId { get; }

    public string WarehouseName { get; }

    public bool IsOpeningAllowed { get; }

    public bool CanReadProducts { get; }

    public bool CanSelectSupplier { get; }

    public StockReceiptType ReceiptType { get; set; } = StockReceiptType.StockIn;

    public SupplierInfo? Supplier { get; set; }

    public string ReferenceNote { get; set; } = string.Empty;

    public List<StockInLineState> Lines { get; } = [new()];

    public List<StockAttachmentDraft> Attachments { get; } = [];

    public bool HasCompleteLine => Lines.Any(line => line.IsComplete);

    public bool HasInvalidLine => Lines.Any(line => line.Product is not null && line.Quantity <= 0);

    public void AddLine() => Lines.Add(new StockInLineState());

    public void RemoveLine(StockInLineState line)
    {
        if (Lines.Count == 1)
        {
            line.Product = null;
            line.Quantity = 1;
            return;
        }

        Lines.Remove(line);
    }

    public void ResetAfterSave()
    {
        ReceiptType = IsOpeningAllowed ? ReceiptType : StockReceiptType.StockIn;
        Supplier = null;
        ReferenceNote = string.Empty;
        Lines.Clear();
        Lines.Add(new StockInLineState());
        Attachments.Clear();
    }
}
