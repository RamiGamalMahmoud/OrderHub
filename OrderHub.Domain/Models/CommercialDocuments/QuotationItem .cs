namespace OrderHub.Domain.Models.CommercialDocuments;

public sealed class QuotationItem : CommercialDocumentItem
{
    private QuotationItem(
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

    private QuotationItem()
    {
    }

    public static QuotationItem Create(
        int productId,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal vatRate,
        decimal subtotal,
        decimal vatAmount,
        decimal total)
    {
        return new QuotationItem(
            productId,
            productName,
            quantity,
            unitPrice,
            vatRate,
            subtotal,
            vatAmount,
            total);
    }

    public int QuotationId { get; private set; }

    public Quotation Quotation { get; private set; } = null!;
}