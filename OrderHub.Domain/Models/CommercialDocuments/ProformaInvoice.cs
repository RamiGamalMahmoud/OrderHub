using System;

namespace OrderHub.Domain.Models.CommercialDocuments;

public class ProformaInvoice : CommercialDocument<ProformaInvoiceItem>
{
    private ProformaInvoice(
        string documentNumber,
        DateTime issueDate,
        string customerName,
        string customerPhone,
        string customerAddress,
        decimal subtotal,
        decimal totalVat,
        decimal totalAmount,
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
        SourceDraftReference = sourceDraftReference;
    }

    private ProformaInvoice()
    {
        
    }

    public static ProformaInvoice Create(
        string documentNumber,
        DateTime issueDate,
        string customerName,
        string customerPhone,
        string customerAddress,
        decimal subtotal,
        decimal totalVat,
        decimal totalAmount,
        Guid sourceDraftReference)
    {
        return new ProformaInvoice(
            documentNumber,
            issueDate,
            customerName,
            customerPhone,
            customerAddress,
            subtotal,
            totalVat,
            totalAmount,
            sourceDraftReference);
    }

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