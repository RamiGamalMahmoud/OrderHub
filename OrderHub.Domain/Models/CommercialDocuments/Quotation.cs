using System;

namespace OrderHub.Domain.Models.CommercialDocuments;

public class Quotation : CommercialDocument<QuotationItem>
{
    private Quotation(
        string documentNumber,
        DateTime issueDate,
        string customerName,
        string customerPhone,
        string customerAddress,
        decimal subtotal,
        decimal totalVat,
        decimal totalAmount,
        DateTime validUntil,
        Guid sourceDraftReference)
        : base(
            documentNumber,
            issueDate,
            customerName,
            customerPhone,
            customerAddress,
            subtotal,
            totalVat,
            totalAmount)
    {
        if (validUntil < issueDate)
            throw new ArgumentException(
                "Quotation validity date cannot be before issue date.",
                nameof(validUntil));

        ValidUntil = validUntil;
        SourceDraftReference = sourceDraftReference;
    }

    private Quotation()
    {
        
    }

    public static Quotation Create(
        string documentNumber,
        DateTime issueDate,
        string customerName,
        string customerPhone,
        string customerAddress,
        decimal subtotal,
        decimal totalVat,
        decimal totalAmount,
        DateTime validUntil,
        Guid sourceDraftReference)
    {
        return new Quotation(
            documentNumber,
            issueDate,
            customerName,
            customerPhone,
            customerAddress,
            subtotal,
            totalVat,
            totalAmount,
            validUntil,
            sourceDraftReference);
    }

    public DateTime ValidUntil { get; private set; }

    public Guid? SourceDraftReference { get; private set; }
    public int? OrderId { get; private set; }
    public Order Order { get; private set; }

    public void LinkToOrder(int orderId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);

        SourceDraftReference = null;
        OrderId = orderId;
    }

    public void LinkToOrder(Order order)
    {
        SourceDraftReference = null;
        Order = order; ;
    }
}