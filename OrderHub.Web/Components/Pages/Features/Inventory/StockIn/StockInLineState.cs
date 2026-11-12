using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Inventory.RecordStockIn;

namespace OrderHub.Web.Components.Pages.Features.Inventory.StockIn;

public sealed class StockInLineState
{
    public ProductLookupItem? Product { get; set; }

    public int Quantity { get; set; } = 1;

    public bool IsComplete => Product is not null && Quantity > 0;

    public StockInLineInput ToInput() => new(Product?.Id ?? 0, Quantity);
}
