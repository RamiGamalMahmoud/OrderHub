using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.GetStockAttachmentFile;

internal sealed class GetStockAttachmentFileQueryHandler(
    IWarehouseRepository warehouses,
    IStockReceiptRepository receipts,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetStockAttachmentFileQuery, Result<StockAttachmentFile>>
{
    public async Task<Result<StockAttachmentFile>> Handle(
        GetStockAttachmentFileQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<StockAttachmentFile>.Failure("يجب تسجيل الدخول لعرض المرفق.", "Unauthorized");

        StockReceiptAttachment attachment = await receipts.GetAttachmentAsync(
            request.AttachmentId,
            cancellationToken);

        if (attachment is null)
            return Result<StockAttachmentFile>.Failure("المرفق غير موجود.", "NotFound");

        Warehouse warehouse = await warehouses.GetByIdAsync(attachment.WarehouseId, cancellationToken);
        bool isKeeper = warehouse is not null && warehouse.KeeperUserId == userId;

        if (!isKeeper &&
            !await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Approve,
                cancellationToken))
        {
            return Result<StockAttachmentFile>.Failure(
                "لا تملك صلاحية عرض مرفق هذا التوريد.",
                "Forbidden");
        }

        return Result<StockAttachmentFile>.Success(new StockAttachmentFile(
            attachment.Id,
            attachment.StoredFileName,
            attachment.OriginalFileName,
            attachment.ContentType));
    }
}
