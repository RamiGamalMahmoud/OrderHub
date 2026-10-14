using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetSummaries;

public sealed record OrderSummaryData(
    string OrderNumber, DateTime OrderDate, string CustomerName, string CustomerPhone,
    string CustomerAddress, string CustomerLocation, decimal InvoiceTotal,
    IReadOnlyList<OrderSummaryLine> Items,
    IReadOnlyList<PartySummaryRecord> DeliveryRecords,
    IReadOnlyList<SupplierSummaryRecord> SupplierRecords);

public sealed record OrderSummaryLine(string ProductName, int Quantity, decimal UnitPrice, decimal VatRate = 0m);
public sealed record PartySummaryRecord(string Kind, int PartyId, long Number, int Year, int Month,
    string PartyName, int? StepOrder, string From, string To);
public sealed record SupplierSummaryRecord(int SupplierId, long Number, int Year, int Month,
    string SupplierName, IReadOnlyList<OrderSummaryLine> Items);

public sealed record GetOrderSummariesQuery(int OrderId) : MediatR.IRequest<OrderSummaryData>;

public sealed record OrderSummaryText(string AudienceType, int PartyId, string AudienceName,
    string Title, string Text);
