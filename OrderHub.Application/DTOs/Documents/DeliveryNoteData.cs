using System.Collections.Generic;

namespace OrderHub.Application.DTOs.Documents;

public record DeliveryNoteData(
    string DocumentNumber,
    int OrderId,
    string OrderNumber,
    string IssueDate,
    DocumentCustomer Customer,
    decimal Subtotal,
    decimal TotalVat,
    decimal TotalAmount,
    IReadOnlyCollection<DeliveryNoteDataItem> Items);

public record DeliveryNoteDataItem(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total);
