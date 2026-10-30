using System;
using OrderHub.Domain.Enums;

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
        Guid sourceDraftReference,
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
        Guid sourceDraftReference,
        decimal deliveryCost = 0m,
        DiscountType discountType = DiscountType.FixedAmount,
        decimal discountValue = 0m,
        decimal discountAmount = 0m)
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
            sourceDraftReference, deliveryCost, discountType, discountValue, discountAmount);
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
