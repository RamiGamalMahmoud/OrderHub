namespace OrderHub.Application.Features.Inventory.RecordStockIn;

public sealed record StockInLineInput(int ProductId, int Quantity);
