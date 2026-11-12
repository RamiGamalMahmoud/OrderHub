using System;

namespace OrderHub.Domain.Models;

public sealed class StockReceiptLine : ModelBase
{
    private StockReceiptLine() { }

    internal StockReceiptLine(int productId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        ProductId = productId;
        Quantity = quantity;
    }

    public int StockReceiptId { get; private set; }

    public StockReceipt StockReceipt { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; }

    public int Quantity { get; private set; }
}
