using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Contracts;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.RecordStockIn;

internal sealed class RecordStockInCommandHandler(
    IWarehouseRepository warehouses,
    IInventoryRepository inventory,
    IStockReceiptRepository receipts,
    IProductRepository products,
    IAuthorizationService authorization,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<RecordStockInCommand, Result<StockReceiptDetails>>
{
    public async Task<Result<StockReceiptDetails>> Handle(
        RecordStockInCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<StockReceiptDetails>.Failure("يجب تسجيل الدخول لتسجيل التوريد.", "Unauthorized");

        if (!await authorization.IsAllowedAsync(
                PermissionFeature.Products,
                PermissionLevel.Read,
                cancellationToken))
        {
            return Result<StockReceiptDetails>.Failure(
                "تحتاج صلاحية قراءة المنتجات لتسجيل التوريد.",
                "Forbidden");
        }

        StockInInput input = request.Input;
        if (input is null || input.Lines is null || input.Lines.Count == 0)
            return Result<StockReceiptDetails>.Failure("أضف صنفًا واحدًا على الأقل للتوريد.");

        if (input.Lines.Any(line => line.ProductId <= 0))
            return Result<StockReceiptDetails>.Failure("أحد المنتجات المحددة غير صالح.");

        if (input.Lines.Any(line => line.Quantity <= 0))
            return Result<StockReceiptDetails>.Failure("كمية السطر يجب أن تكون أكبر من صفر.");

        Warehouse warehouse = await warehouses.GetByIdAsync(input.WarehouseId, cancellationToken);
        if (warehouse is null)
            return Result<StockReceiptDetails>.Failure("المخزن غير موجود.", "NotFound");

        if (!warehouse.IsActive)
            return Result<StockReceiptDetails>.Failure("المخزن غير نشط؛ لا يمكن تسجيل توريد عليه.");

        if (warehouse.KeeperUserId != userId)
        {
            return Result<StockReceiptDetails>.Failure(
                "تسجيل التوريد متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        if (!Enum.IsDefined(input.ReceiptType))
            return Result<StockReceiptDetails>.Failure("نوع التوريد غير صالح.");

        if (input.ReceiptType == StockReceiptType.Opening &&
            await inventory.HasMovementsAsync(warehouse.Id, cancellationToken))
        {
            return Result<StockReceiptDetails>.Failure(
                "الرصيد الافتتاحي سُجّل لهذا المخزن مسبقًا. اختر نوع «توريد».");
        }

        int[] productIds = input.Lines.Select(line => line.ProductId).Distinct().ToArray();
        IReadOnlyList<Product> loadedProducts = await products.GetByIdsAsync(productIds, cancellationToken);
        Dictionary<int, Product> productsById = loadedProducts.ToDictionary(product => product.Id);

        if (productsById.Count != productIds.Length)
            return Result<StockReceiptDetails>.Failure("أحد المنتجات المحددة غير موجود.");

        int? supplierId = input.SupplierId;
        if (supplierId is int requestedSupplierId)
        {
            IReadOnlyList<Supplier> suppliers = await products.GetSuppliersAsync(
                [requestedSupplierId],
                cancellationToken);

            Supplier supplier = suppliers.FirstOrDefault();
            if (supplier is null)
                return Result<StockReceiptDetails>.Failure("المورد غير موجود.");

            foreach (StockInLineInput line in input.Lines)
            {
                Product product = productsById[line.ProductId];
                if (product.Suppliers.All(candidate => candidate.Id != supplier.Id))
                {
                    return Result<StockReceiptDetails>.Failure(
                        $"المنتج «{product.Name.Value}» غير مرتبط بالمورد المحدد.");
                }
            }
        }

        int[] attachmentIds = (input.AttachmentIds ?? []).Where(id => id > 0).Distinct().ToArray();
        IReadOnlyList<StockReceiptAttachment> draftAttachments = await receipts.GetDraftAttachmentsAsync(
            attachmentIds,
            warehouse.Id,
            userId,
            cancellationToken);

        if (draftAttachments.Count != attachmentIds.Length)
        {
            return Result<StockReceiptDetails>.Failure(
                "أحد المرفقات غير متاح أو تم ربطه بتوريد آخر. أعد رفع الملف.");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;

        StockReceipt receipt;
        try
        {
            receipt = StockReceipt.Create(
                warehouse.Id,
                input.ReceiptType,
                supplierId,
                input.ReferenceNote,
                userId,
                input.Lines.Select(line => new StockReceiptLineData(line.ProductId, line.Quantity)),
                now);
        }
        catch (ArgumentException exception)
        {
            return Result<StockReceiptDetails>.Failure(exception.Message);
        }
        catch (OverflowException)
        {
            return Result<StockReceiptDetails>.Failure("كميات التوريد كبيرة جدًا.");
        }

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            receipts.Add(receipt);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            InventoryMovementType movementType = receipt.ReceiptType == StockReceiptType.Opening
                ? InventoryMovementType.OpeningStock
                : InventoryMovementType.StockIn;

            foreach (StockReceiptLine line in receipt.Lines)
            {
                StockBalance balance = await inventory.GetOrCreateStockItemAsync(
                    warehouse.Id,
                    line.ProductId,
                    cancellationToken);

                balance.ApplyIn(line.Quantity);

                inventory.Add(InventoryMovement.Create(
                    warehouse.Id,
                    line.ProductId,
                    line.Quantity,
                    InventoryDirection.In,
                    movementType,
                    now,
                    userId,
                    note: receipt.ReferenceNote,
                    sourceStockReceiptId: receipt.Id,
                    nowUtc: now));
            }

            foreach (StockReceiptAttachment attachment in draftAttachments)
                receipt.Attach(attachment);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<StockReceiptDetails>.Failure(
                "حدث تعارض أثناء حفظ التوريد. يرجى المحاولة مرة أخرى.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<StockReceiptDetails>.Failure(
                "تعذر حفظ التوريد. يرجى المحاولة مرة أخرى.",
                "Persistence");
        }
        catch (ArgumentException exception)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<StockReceiptDetails>.Failure(exception.Message, "InvalidOperation");
        }
        catch (OverflowException)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<StockReceiptDetails>.Failure("كميات التوريد كبيرة جدًا.", "InvalidOperation");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return Result<StockReceiptDetails>.Success(new StockReceiptDetails(
            receipt.Id,
            warehouse.Id,
            warehouse.Name.Value,
            receipt.ReceiptType,
            receipt.SupplierId,
            receipt.ReferenceNote,
            receipt.CreatedAt,
            currentUser.DisplayName ?? currentUser.UserName ?? string.Empty,
            receipt.Lines
                .OrderBy(line => line.ProductId)
                .Select(line => new StockReceiptLineDetails(
                    line.ProductId,
                    productsById[line.ProductId].Name.Value,
                    line.Quantity))
                .ToList(),
            draftAttachments
                .OrderBy(attachment => attachment.Id)
                .Select(attachment => new StockAttachmentDetails(
                    attachment.Id,
                    attachment.OriginalFileName,
                    attachment.Description,
                    attachment.SizeBytes,
                    attachment.ContentType))
                .ToList()));
    }
}
