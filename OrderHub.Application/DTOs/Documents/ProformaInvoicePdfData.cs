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
    IReadOnlyCollection<ProformaInvoicePdfDataDataItem> Items,
    decimal DeliveryCost = 0m,
    OrderHub.Domain.Enums.DiscountType DiscountType = OrderHub.Domain.Enums.DiscountType.FixedAmount,
    decimal DiscountValue = 0m,
    decimal DiscountAmount = 0m)
{
    public decimal ProductsTotalIncludingVat => Subtotal + TotalVat;
}

public record ProformaInvoicePdfDataDataItem(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total);

