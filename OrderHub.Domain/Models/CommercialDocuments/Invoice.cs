using System;

namespace OrderHub.Domain.Models.CommercialDocuments;


public class Invoice : CommercialDocument<InvoiceItem>
{
    private Invoice(
        string documentNumber,
        DateTime issueDate,
        string customerName,
        string customerPhone,
        string customerAddress,
        decimal subtotal,
        decimal totalVat,
        decimal totalAmount,
        int orderId)
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
        OrderId = orderId;
    }

    private Invoice()
    {

    }

    public static Invoice Create(
        string documentNumber,
        DateTime issueDate,
        string customerName,
        string customerPhone,
        string customerAddress,
        decimal subtotal,
        decimal totalVat,
        decimal totalAmount,
        int orderId)
    {
        return new Invoice(
            documentNumber,
            issueDate,
            customerName,
            customerPhone,
            customerAddress,
            subtotal,
            totalVat,
            totalAmount,
            orderId);
    }

    public int OrderId { get; private set; }
    public Order Order { get; private set; }
}