using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.GetStockAttachmentFile;

[RequirePermission(PermissionFeature.Inventory, PermissionLevel.Read)]
public sealed record GetStockAttachmentFileQuery(int AttachmentId)
    : IRequest<Result<StockAttachmentFile>>;

public sealed record StockAttachmentFile(
    int Id,
    string StoredFileName,
    string OriginalFileName,
    string ContentType);
