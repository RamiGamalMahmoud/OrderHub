namespace OrderHub.Domain.Models.CommercialDocuments;

public sealed class InvoiceItem : CommercialDocumentItem
{
    public InvoiceItem(
        int productId,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal vatRate,
        decimal subtotal,
        decimal vatAmount,
        decimal total)
        : base(
            productId,
            productName,
            quantity,
            unitPrice,
            vatRate,
            subtotal,
            vatAmount,
            total)
    {
    }

    private InvoiceItem()
    {
    }

    public int InvoiceId { get; private set; }

    public Invoice Invoice { get; private set; } = null!;
}