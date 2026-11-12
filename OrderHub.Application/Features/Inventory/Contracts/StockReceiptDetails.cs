using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Contracts;

public sealed record StockReceiptDetails(
    int Id,
    int WarehouseId,
    string WarehouseName,
    StockReceiptType ReceiptType,
    int? SupplierId,
    string ReferenceNote,
    DateTime CreatedAtUtc,
    string CreatedByDisplayName,
    IReadOnlyList<StockReceiptLineDetails> Lines,
    IReadOnlyList<StockAttachmentDetails> Attachments);

public sealed record StockReceiptLineDetails(
    int ProductId,
    string ProductName,
    int Quantity);

public sealed record StockAttachmentDetails(
    int Id,
    string OriginalFileName,
    string Description,
    long SizeBytes,
    string ContentType);
