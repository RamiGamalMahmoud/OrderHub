using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.GetSummaries;

public sealed record OrderSummaryData(
    string OrderNumber, DateTime OrderDate,
    string CustomerName, string CustomerPhone, string CustomerAddress, string CustomerLocation, string CustomerCity,
    decimal InvoiceTotal, string PaymentMethodCode, string PaymentMethodName,
    IReadOnlyList<OrderSummaryLine> Items,
    IReadOnlyList<PartySummaryRecord> DeliveryRecords,
    IReadOnlyList<SupplierSummaryRecord> SupplierRecords);

public sealed record OrderSummaryLine(string ProductName, int Quantity, decimal UnitPrice, decimal VatRate = 0m);
public sealed record PartySummaryRecord(string Kind, int PartyId, long Number, int Year, int Month,
    string PartyName, int? StepOrder, string From, string To,
    string PartyPhone = null);
public sealed record SupplierSummaryRecord(int SupplierId, long Number, int Year, int Month,
    string SupplierName, IReadOnlyList<OrderSummaryLine> Items,
    string SupplierPhone = null, string SupplierAddress = null);

public sealed record GetOrderSummariesQuery(int OrderId) : MediatR.IRequest<OrderSummaryData>;

public sealed record OrderSummaryText(string AudienceType, int PartyId, string AudienceName,
    string Title, string Text);

/// <summary>A sendable recipient: the party details for the dispatch card plus its message records.</summary>
public sealed record OrderSummaryAudience(
    string AudienceType, int PartyId, string Name, string RoleLabel, string BadgeLabel,
    string Phone, string Destination, string CollectionLabel, decimal? CollectionAmount,
    IReadOnlyList<OrderSummaryText> Records);
