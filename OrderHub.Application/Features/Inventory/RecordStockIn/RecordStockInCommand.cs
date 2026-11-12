using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.RecordStockIn;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record RecordStockInCommand(StockInInput Input)
    : IRequest<Result<StockReceiptDetails>>;

public sealed record StockInInput(
    int WarehouseId,
    StockReceiptType ReceiptType,
    int? SupplierId,
    string ReferenceNote,
    IReadOnlyList<StockInLineInput> Lines,
    IReadOnlyList<int> AttachmentIds);
