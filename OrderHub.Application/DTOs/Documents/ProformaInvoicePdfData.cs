using System;
using System.Collections.Generic;

namespace OrderHub.Application.DTOs.Documents;

public record ProformaInvoicePdfData(
    string DocumentNumber,
    Guid? ReferenceNumber,
    int? OrderId,
    string OrderNumber,
    string IssueDate,
    DocumentCustomer Customer,
    decimal Subtotal,
    decimal TotalVat,
    decimal TotalAmount,
    IReadOnlyCollection<ProformaInvoicePdfDataDataItem> Items);

public record ProformaInvoicePdfDataDataItem(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total);

