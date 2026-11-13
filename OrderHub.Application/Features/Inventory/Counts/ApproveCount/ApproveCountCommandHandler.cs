using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Counts.ApproveCount;

internal sealed class ApproveCountCommandHandler(
    IInventoryCountRepository counts,
    IInventoryRepository inventory,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<ApproveCountCommand, Result<CountDecisionResult>>
{
    public async Task<Result<CountDecisionResult>> Handle(
        ApproveCountCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<CountDecisionResult>.Failure("يجب تسجيل الدخول لاعتماد الجرد.", "Unauthorized");

        if (request.ExpectedVersion < 0)
            return Result<CountDecisionResult>.Failure("إصدار الجرد غير صالح.");

        DateTime now = clock.GetUtcNow().UtcDateTime;

        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            InventoryCount count = await counts.GetForUpdateAsync(request.CountId, cancellationToken);
            if (count is null)
                return Result<CountDecisionResult>.Failure("الجرد غير موجود.", "NotFound");

            if (count.Status != InventoryCountStatus.UnderReview)
            {
                return Result<CountDecisionResult>.Failure(
                    "لا يمكن اعتماد جرد لم يُنهَ عدّه.",
                    "Conflict");
            }

            if (count.Version != request.ExpectedVersion)
            {
                return Result<CountDecisionResult>.Failure(
                    "تغيّر الجرد منذ فتحه؛ حدّث الصفحة وأعد المحاولة.",
                    "Conflict");
            }

            IReadOnlyList<int> lateLineIds = await counts.GetLateMovementLineIdsAsync(count, cancellationToken);
            if (lateLineIds.Count > 0)
            {
                return Result<CountDecisionResult>.Failure(
                    "سُجلت حركات بتاريخ سابق لِلقطة الجرد بعد إنهاء العدّ؛ راجعها قبل الاعتماد.",
                    "ApprovalRequired");
            }

            List<InventoryCountLine> lines = count.Lines
                .Where(line => line.IsActive)
                .OrderBy(line => line.ProductId)
                .ToList();

            if (lines.Any(line => !line.IsCounted))
            {
                return Result<CountDecisionResult>.Failure(
                    "لا يمكن الاعتماد قبل إدخال الكمية السليمة والتالفة لكل صنف.",
                    "Conflict");
            }

            await inventory.LockBalancesAsync(
                lines.Select(line => (count.WarehouseId, line.ProductId)).Distinct().ToList(),
                cancellationToken);

            string movementNote = string.IsNullOrWhiteSpace(request.Note)
                ? $"تسوية جرد رقم {count.Id.ToString(CultureInfo.InvariantCulture)}"
                : request.Note.Trim();

            foreach (InventoryCountLine line in lines)
            {
                int soundDelta = line.Delta ?? 0;
                int damagedDelta = line.DamagedDelta ?? 0;
                if (soundDelta == 0 && damagedDelta == 0)
                    continue;

                string productName = line.Product?.Name?.Value ?? "صنف";
                StockBalance balance = await inventory.GetOrCreateStockItemAsync(
                    count.WarehouseId,
                    line.ProductId,
                    cancellationToken);

                if (soundDelta < 0 && balance.StockOnHand + soundDelta < balance.ReservedOnHand)
                {
                    return Result<CountDecisionResult>.Failure(
                        $"لا يمكن اعتماد جرد الصنف «{productName}»: الكمية السليمة الناتجة " +
                        $"{Format(balance.StockOnHand + soundDelta)} أقل من الكمية المحجوزة " +
                        $"{Format(balance.ReservedOnHand)}.",
                        "Conflict");
                }

                if (damagedDelta < 0 && balance.DamagedQty + damagedDelta < 0)
                {
                    return Result<CountDecisionResult>.Failure(
                        $"لا يمكن اعتماد جرد الصنف «{productName}»: الكمية التالفة الناتجة ستكون سالبة.",
                        "Conflict");
                }

                if (soundDelta > 0)
                {
                    balance.ApplyIn(soundDelta);
                    inventory.Add(InventoryMovement.Create(
                        count.WarehouseId,
                        line.ProductId,
                        soundDelta,
                        InventoryDirection.In,
                        InventoryMovementType.Adjustment,
                        now,
                        userId,
                        note: movementNote,
                        sourceCountId: count.Id,
                        nowUtc: now));
                }
                else if (soundDelta < 0)
                {
                    balance.ApplyOut(-soundDelta);
                    inventory.Add(InventoryMovement.Create(
                        count.WarehouseId,
                        line.ProductId,
                        -soundDelta,
                        InventoryDirection.Out,
                        InventoryMovementType.Adjustment,
                        now,
                        userId,
                        note: movementNote,
                        sourceCountId: count.Id,
                        nowUtc: now));
                }

                if (damagedDelta > 0)
                {
                    balance.DamagedIn(damagedDelta);
                    inventory.Add(InventoryMovement.Create(
                        count.WarehouseId,
                        line.ProductId,
                        damagedDelta,
                        InventoryDirection.In,
                        InventoryMovementType.DamagedAdjustment,
                        now,
                        userId,
                        note: movementNote,
                        sourceCountId: count.Id,
                        nowUtc: now));
                }
                else if (damagedDelta < 0)
                {
                    balance.DamagedAdjust(damagedDelta);
                    inventory.Add(InventoryMovement.Create(
                        count.WarehouseId,
                        line.ProductId,
                        -damagedDelta,
                        InventoryDirection.Out,
                        InventoryMovementType.DamagedAdjustment,
                        now,
                        userId,
                        note: movementNote,
                        sourceCountId: count.Id,
                        nowUtc: now));
                }
            }

            Result approved = count.Approve(userId, request.Note, now);
            if (!approved.IsSuccess)
                return Result<CountDecisionResult>.Failure(approved.ErrorMessage, approved.ErrorCode);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result<CountDecisionResult>.Success(
                new CountDecisionResult(count.Id, count.Version, count.Status));
        }
        catch (PersistenceConflictException)
        {
            return Result<CountDecisionResult>.Failure(
                "اعتُمد هذا الجرد أو تغيّر من مستخدم آخر؛ حدّث القائمة.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<CountDecisionResult>.Failure(
                "تعذر اعتماد الجرد. يرجى المحاولة مرة أخرى.",
                "Persistence");
        }
        catch (ArgumentException exception)
        {
            return Result<CountDecisionResult>.Failure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<CountDecisionResult>.Failure(exception.Message, "Conflict");
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
