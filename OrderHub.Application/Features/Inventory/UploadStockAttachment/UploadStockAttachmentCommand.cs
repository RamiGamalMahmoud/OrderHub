using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.UploadStockAttachment;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Write)]
public sealed record UploadStockAttachmentCommand(
    int WarehouseId,
    string OriginalFileName,
    string Description,
    byte[] Content) : IRequest<Result<StockAttachmentDraft>>;

public sealed record StockAttachmentDraft(
    int Id,
    string OriginalFileName,
    string Description,
    long SizeBytes,
    string ContentType);
