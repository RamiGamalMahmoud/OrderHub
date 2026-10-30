using System;
using OrderHub.Domain.Enums;

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
        int orderId,
        decimal deliveryCost,
        DiscountType discountType,
        decimal discountValue,
        decimal discountAmount)
        : base(
            documentNumber,
            issueDate,
            customerName,
            customerPhone,
            customerAddress,
            subtotal,
            totalVat,
            totalAmount, deliveryCost, discountType, discountValue, discountAmount)
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
        int orderId,
        decimal deliveryCost = 0m,
        DiscountType discountType = DiscountType.FixedAmount,
        decimal discountValue = 0m,
        decimal discountAmount = 0m)
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
            orderId, deliveryCost, discountType, discountValue, discountAmount);
    }

    public int OrderId { get; private set; }
    public Order Order { get; private set; }
}
