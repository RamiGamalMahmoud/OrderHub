using System;
using System.Collections.Generic;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models.CommercialDocuments;

public abstract class CommercialDocument<TItem> : ModelBase where TItem : CommercialDocumentItem
{
    private readonly List<TItem> _items = [];
    protected CommercialDocument()
    {
        
    }
    protected CommercialDocument(
        string documentNumber,
        DateTime issueDate,
        string customerName,
        string customerPhone,
        string customerAddress,
        decimal subtotal,
        decimal totalVat,
        decimal totalAmount,
        decimal deliveryCost = 0m,
        DiscountType discountType = DiscountType.FixedAmount,
        decimal discountValue = 0m,
        decimal discountAmount = 0m)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.", nameof(documentNumber));

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));

        DocumentNumber = documentNumber;
        IssueDate = issueDate;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        CustomerAddress = customerAddress;
        Subtotal = subtotal;
        TotalVat = totalVat;
        TotalAmount = totalAmount;
        DeliveryCost = deliveryCost;
        DiscountType = discountType;
        DiscountValue = discountValue;
        DiscountAmount = discountAmount;
    }

    public string DocumentNumber { get; private set; }

    public DateTime IssueDate { get; private set; }

    public string CustomerName { get; private set; }

    public string CustomerPhone { get; private set; }
    public string CustomerAddress { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal TotalVat { get; private set; }

    public decimal TotalAmount { get; private set; }
    public decimal DeliveryCost { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal DiscountAmount { get; private set; }

    public IReadOnlyCollection<TItem> Items =>
        _items.AsReadOnly();

    public void AddItem(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _items.Add(item);
    }

    protected void SetTotals(
        decimal subtotal,
        decimal totalVat,
        decimal totalAmount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(subtotal);

        ArgumentOutOfRangeException.ThrowIfNegative(totalVat);

        ArgumentOutOfRangeException.ThrowIfNegative(totalAmount);

        Subtotal = subtotal;
        TotalVat = totalVat;
        TotalAmount = totalAmount;
    }
}
