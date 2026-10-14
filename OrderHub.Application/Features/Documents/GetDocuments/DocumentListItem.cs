#nullable enable
using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Features.Documents.GetDocuments;

public sealed record DocumentListItem(
    int Id,
    DocumentType Type,
    string DocumentNumber,
    DateTime IssueDate,
    string CustomerName,
    int? OrderId,
    string? OrderNumber,
    decimal TotalVat,
    decimal TotalAmount,
    DateTime? ValidUntil = null);
