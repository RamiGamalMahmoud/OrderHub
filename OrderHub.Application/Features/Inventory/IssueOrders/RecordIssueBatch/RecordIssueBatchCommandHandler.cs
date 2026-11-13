using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.IssueOrders.RecordIssueBatch;

internal sealed class RecordIssueBatchCommandHandler(
    IStockIssueOrderRepository issueOrders,
    IInventoryRepository inventory,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<RecordIssueBatchCommand, Result>
{
    public async Task<Result> Handle(
        RecordIssueBatchCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result.Failure("يجب تسجيل الدخول لتسجيل الصرف.", "Unauthorized");

        if (request.Lines is null || request.Lines.Count == 0)
            return Result.Failure("أضف كمية واحدة على الأقل للدفعة.");
        if (request.Lines.Any(line => line is null || line.LineId <= 0 || line.Quantity <= 0))
            return Result.Failure("بنود الدفعة أو كمياتها غير صالحة.");
        if (request.Lines.Select(line => line.LineId).Distinct().Count() != request.Lines.Count)
            return Result.Failure("لا يمكن تكرار البند نفسه في الدفعة.");

        StockIssueOrder issueOrder = await issueOrders.GetForUpdateAsync(
            request.IssueOrderId,
            cancellationToken);
        if (issueOrder is null)
            return Result.Failure("أمر الصرف غير موجود.", "NotFound");

        if (issueOrder.Warehouse.KeeperUserId != userId)
        {
            return Result.Failure(
                "تسجيل الصرف متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        if (!issueOrder.AcceptsBatches)
        {
            return Result.Failure(
                issueOrder.Status == WorkRecordStatus.CancelRequested
                    ? "طُلب إلغاء أمر الصرف ولا يمكن تسجيل دفعات جديدة."
                    : "أمر الصرف مغلق ولا يمكن تسجيل دفعات جديدة.",
                "InvalidOperation");
        }

        var linesById = issueOrder.Lines.ToDictionary(line => line.Id);
        if (request.Lines.Any(line => !linesById.ContainsKey(line.LineId)))
            return Result.Failure("أحد بنود أمر الصرف غير موجود.", "NotFound");

        DateTime now = clock.GetUtcNow().UtcDateTime;
        DateTime effectiveAtUtc = request.EffectiveAtUtc == default
            ? now
            : DateTime.SpecifyKind(request.EffectiveAtUtc, DateTimeKind.Utc);

        var existingBatchIds = issueOrder.Lines
            .SelectMany(line => line.Batches)
            .Select(batch => batch.Id)
            .ToHashSet();

        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            await inventory.LockBalancesAsync(
                request.Lines
                    .Select(line => (
                        ProductId: linesById[line.LineId].ProductId,
                        WarehouseId: issueOrder.WarehouseId))
                    .ToList(),
                cancellationToken);

            Result recorded = issueOrder.RecordBatches(
                request.Lines
                    .Select(line => new StockIssueBatchData(
                        line.LineId,
                        line.Quantity,
                        line.ShortfallReason))
                    .ToList(),
                userId,
                effectiveAtUtc,
                request.Final,
                now);

            if (!recorded.IsSuccess)
                return recorded;

            foreach (IssueBatchLineInput line in request.Lines)
            {
                Result consumed = linesById[line.LineId].Allocation.Consume(line.Quantity);
                if (!consumed.IsSuccess)
                    return consumed;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (StockIssueLine line in issueOrder.Lines)
            {
                foreach (StockIssueBatch batch in line.Batches
                             .Where(batch => batch.Id > 0 && !existingBatchIds.Contains(batch.Id)))
                {
                    inventory.Add(InventoryMovement.Create(
                        issueOrder.WarehouseId,
                        line.ProductId,
                        batch.Quantity,
                        InventoryDirection.Out,
                        InventoryMovementType.IssueOut,
                        batch.EffectiveAtUtc,
                        userId,
                        note: batch.ShortfallReason,
                        sourceIssueBatchId: batch.Id,
                        nowUtc: now));

                    if (!await inventory.ConsumeAsync(
                            issueOrder.WarehouseId,
                            line.ProductId,
                            batch.Quantity,
                            cancellationToken))
                    {
                        return Result.Failure(
                            "تعذر صرف الكمية من الرصيد المحجوز؛ حدّث الأمر ثم أعد المحاولة.",
                            "Conflict");
                    }
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success();
        }
        catch (ArgumentException exception)
        {
            return Result.Failure(exception.Message);
        }
        catch (PersistenceConflictException)
        {
            return Result.Failure(
                "حدث تعارض أثناء حفظ الدفعة. حدّث أمر الصرف ثم أعد المحاولة.",
                "Conflict");
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ الدفعة. يرجى المحاولة مرة أخرى.", "Persistence");
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }
}
