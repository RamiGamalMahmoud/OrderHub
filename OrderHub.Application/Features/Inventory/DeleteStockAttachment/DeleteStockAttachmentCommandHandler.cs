using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.DeleteStockAttachment;

internal sealed class DeleteStockAttachmentCommandHandler(
    IWarehouseRepository warehouses,
    IStockReceiptRepository receipts,
    ILocalFileAccessService files,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteStockAttachmentCommand, Result>
{
    public async Task<Result> Handle(
        DeleteStockAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result.Failure("يجب تسجيل الدخول لحذف المرفق.", "Unauthorized");

        StockReceiptAttachment attachment = await receipts.GetAttachmentAsync(
            request.AttachmentId,
            cancellationToken);

        if (attachment is null)
            return Result.Failure("المرفق غير موجود.", "NotFound");

        Warehouse warehouse = await warehouses.GetByKeeperAsync(userId, cancellationToken);
        if (warehouse is null ||
            warehouse.Id != attachment.WarehouseId ||
            attachment.UploadedByUserId != userId)
        {
            return Result.Failure(
                "حذف المرفق متاح لأمين المخزن الذي رفعه فقط.",
                "Forbidden");
        }

        if (!attachment.IsDraft)
            return Result.Failure("لا يمكن حذف مرفق مرتبط بتوريد محفوظ.");

        receipts.RemoveAttachment(attachment);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            return Result.Failure("حدث تعارض أثناء حذف المرفق. يرجى المحاولة مرة أخرى.", "Conflict");
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حذف المرفق. يرجى المحاولة مرة أخرى.", "Persistence");
        }

        try
        {
            await files.DeleteStockAttachmentAsync(attachment.StoredFileName);
        }
        catch (IOException)
        {
            // The metadata row is gone; a leftover file must not block the delete.
        }
        catch (UnauthorizedAccessException)
        {
        }

        return Result.Success();
    }
}
