using System;
using System.Collections.Generic;

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
        decimal totalAmount)
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
    }

    public string DocumentNumber { get; private set; }

    public DateTime IssueDate { get; private set; }

    public string CustomerName { get; private set; }

    public string CustomerPhone { get; private set; }
    public string CustomerAddress { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal TotalVat { get; private set; }

    public decimal TotalAmount { get; private set; }

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