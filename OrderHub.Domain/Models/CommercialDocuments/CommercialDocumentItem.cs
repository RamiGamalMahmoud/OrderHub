using System;

namespace OrderHub.Domain.Models.CommercialDocuments;

public abstract class CommercialDocumentItem : ModelBase
{
    protected CommercialDocumentItem(
        int productId,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal vatRate,
        decimal subtotal,
        decimal vatAmount,
        decimal total)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(
                "Product name is required.",
                nameof(productName));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(vatRate);
        ArgumentOutOfRangeException.ThrowIfNegative(subtotal);
        ArgumentOutOfRangeException.ThrowIfNegative(vatAmount);
        ArgumentOutOfRangeException.ThrowIfNegative(total);

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        VatRate = vatRate;
        Subtotal = subtotal;
        VatAmount = vatAmount;
        Total = total;
    }

    protected CommercialDocumentItem()
    {
    }

    public int ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string ProductName { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal VatRate { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal VatAmount { get; private set; }

    public decimal Total { get; private set; }
}