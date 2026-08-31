using System.Collections.Generic;

namespace OrderHub.Application.DTOs.Documents;

public record InvoicePdfData(
    string DocumentNumber,
    int OrderId,
    string OrderNumber,
    string IssueDate,
    DocumentCustomer Customer,
    decimal Subtotal,
    decimal TotalVat,
    decimal TotalAmount,
    IReadOnlyCollection<InvoicePdfDataItem> Items);

public record InvoicePdfDataItem(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total);
