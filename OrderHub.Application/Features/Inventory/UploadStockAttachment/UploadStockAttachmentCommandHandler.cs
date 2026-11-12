using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.UploadStockAttachment;

internal sealed class UploadStockAttachmentCommandHandler(
    IWarehouseRepository warehouses,
    IStockReceiptRepository receipts,
    ILocalFileAccessService files,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<UploadStockAttachmentCommand, Result<StockAttachmentDraft>>
{
    private static readonly TimeSpan StaleDraftLifetime = TimeSpan.FromHours(24);

    public async Task<Result<StockAttachmentDraft>> Handle(
        UploadStockAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<StockAttachmentDraft>.Failure("يجب تسجيل الدخول لرفع المرفقات.", "Unauthorized");

        Warehouse warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
            return Result<StockAttachmentDraft>.Failure("المخزن غير موجود.", "NotFound");

        if (!warehouse.IsActive)
            return Result<StockAttachmentDraft>.Failure("المخزن غير نشط؛ لا يمكن رفع مرفقات عليه.");

        if (warehouse.KeeperUserId != userId)
        {
            return Result<StockAttachmentDraft>.Failure(
                "رفع مرفقات التوريد متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        string description = StockAttachmentRules.NormalizeDescription(request.Description, out string descriptionError);
        if (descriptionError is not null)
            return Result<StockAttachmentDraft>.Failure(descriptionError);

        string originalFileName = request.OriginalFileName?.Trim() ?? string.Empty;
        if (originalFileName.Length == 0 ||
            originalFileName.Length > StockReceiptAttachment.OriginalFileNameMaxLength)
        {
            return Result<StockAttachmentDraft>.Failure("اسم الملف غير صالح.");
        }

        if (!StockAttachmentRules.TryResolve(
                request.Content,
                originalFileName,
                out string contentType,
                out string extension,
                out string fileError))
        {
            return Result<StockAttachmentDraft>.Failure(fileError);
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;

        string storedFileName = Guid.NewGuid().ToString("N") + extension;
        StockReceiptAttachment attachment;

        try
        {
            await using var stream = new MemoryStream(request.Content, writable: false);
            await files.SaveStockAttachmentAsync(storedFileName, stream);

            attachment = StockReceiptAttachment.CreateDraft(
                warehouse.Id,
                storedFileName,
                originalFileName,
                description,
                request.Content.Length,
                contentType,
                userId);

            receipts.AddAttachment(attachment);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            await files.DeleteStockAttachmentAsync(storedFileName);
            return Result<StockAttachmentDraft>.Failure(
                "حدث تعارض أثناء حفظ المرفق. يرجى المحاولة مرة أخرى.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            await files.DeleteStockAttachmentAsync(storedFileName);
            return Result<StockAttachmentDraft>.Failure(
                "تعذر حفظ المرفق. يرجى المحاولة مرة أخرى.",
                "Persistence");
        }
        catch
        {
            await files.DeleteStockAttachmentAsync(storedFileName);
            throw;
        }

        try
        {
            await SweepStaleDraftsAsync(warehouse.Id, userId, now, cancellationToken);
        }
        catch (PersistenceException)
        {
            // Best-effort cleanup; the uploaded attachment is already saved.
        }

        return Result<StockAttachmentDraft>.Success(new StockAttachmentDraft(
            attachment.Id,
            attachment.OriginalFileName,
            attachment.Description,
            attachment.SizeBytes,
            attachment.ContentType));
    }

    private async Task SweepStaleDraftsAsync(
        int warehouseId,
        int userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StockReceiptAttachment> stale = await receipts.GetStaleDraftsAsync(
            warehouseId,
            userId,
            now.Subtract(StaleDraftLifetime),
            cancellationToken);

        if (stale.Count == 0)
            return;

        foreach (StockReceiptAttachment draft in stale)
            receipts.RemoveAttachment(draft);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (StockReceiptAttachment draft in stale)
        {
            try
            {
                await files.DeleteStockAttachmentAsync(draft.StoredFileName);
            }
            catch (IOException)
            {
                // Best-effort cleanup; the metadata row is already gone.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
