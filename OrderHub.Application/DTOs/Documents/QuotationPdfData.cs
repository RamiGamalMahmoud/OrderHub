using System;
using System.Collections.Generic;

namespace OrderHub.Application.DTOs.Documents;

public record QuotationPdfData(
    string DocumentNumber,
    Guid? ReferenceNumber,
    int? OrderId,
    string OrderNumber,
    string IssueDate,
    DocumentCustomer Customer,
    decimal Subtotal,
    decimal TotalVat,
    decimal TotalAmount,
    IReadOnlyCollection<QuotationPdfDataItem> Items);

public record QuotationPdfDataItem(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total);

