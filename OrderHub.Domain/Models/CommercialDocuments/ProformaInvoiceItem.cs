namespace OrderHub.Domain.Models.CommercialDocuments;

public sealed class ProformaInvoiceItem : CommercialDocumentItem
{
    private ProformaInvoiceItem(
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

    private ProformaInvoiceItem()
    {
    }

    public static ProformaInvoiceItem Create(
        int productId,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal vatRate,
        decimal subtotal,
        decimal vatAmount,
        decimal total)
    {
        return new ProformaInvoiceItem(
            productId,
            productName,
            quantity,
            unitPrice,
            vatRate,
            subtotal,
            vatAmount,
            total);
    }

    public int ProformaInvoiceId { get; private set; }

    public ProformaInvoice ProformaInvoice { get; private set; } = null!;
}
