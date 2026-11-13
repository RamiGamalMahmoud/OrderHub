using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Approvals.DecideApproval;

internal sealed class DecideApprovalCommandHandler(
    IInventoryApprovalRepository approvals,
    IOrderRepository orders,
    IStockIssueOrderRepository issueOrders,
    IInventoryRepository inventory,
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<DecideApprovalCommand, Result>
{
    private const string StaleCode = "Stale";

    public async Task<Result> Handle(DecideApprovalCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result.Failure("يجب تسجيل الدخول لاعتماد الطلبات.", "Unauthorized");

        InventoryApprovalRequest approval = await approvals.GetForUpdateAsync(
            request.ApprovalId,
            cancellationToken);
        if (approval is null)
            return Result.Failure("طلب الاعتماد غير موجود.", "NotFound");

        if (approval.Status != InventoryApprovalStatus.Pending ||
            approval.Version != request.ExpectedVersion)
            return Result.Failure("تمت معالجة هذا الطلب أو تغيّر منذ فتحه؛ حدّث القائمة.", "Conflict");

        // The requester may only approve their own movement reversal (the documented exception).
        if (approval.RequestType != InventoryApprovalRequestType.MovementReversal &&
            approval.RequestedByUserId == userId)
            return Result.Failure("لا يمكن للطالب اعتماد طلبه؛ يلزم اعتماد مدير آخر.", "Forbidden");

        DateTime now = clock.GetUtcNow().UtcDateTime;

        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            Result outcome = request.Approve
                ? await ApproveAsync(approval, userId, request.Note, now, cancellationToken)
                : await RejectAsync(approval, userId, request.Note, now, cancellationToken);

            if (!outcome.IsSuccess && outcome.ErrorCode != StaleCode)
                return outcome;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return outcome;
        }
        catch (PersistenceConflictException)
        {
            return Result.Failure("تمت معالجة هذا الطلب من مستخدم آخر؛ حدّث القائمة.", "Conflict");
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر تنفيذ القرار. حاول مرة أخرى.", "Persistence");
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure(exception.Message, "Conflict");
        }
        catch (ArgumentException exception)
        {
            return Result.Failure(exception.Message);
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }

    private Task<Result> ApproveAsync(
        InventoryApprovalRequest approval,
        int userId,
        string note,
        DateTime now,
        CancellationToken cancellationToken) =>
        approval.RequestType switch
        {
            InventoryApprovalRequestType.AllocationEdit =>
                ApproveEditAsync(approval, userId, note, now, cancellationToken),
            InventoryApprovalRequestType.IssueOrderCancel =>
                ApproveCancelAsync(approval, userId, note, now, cancellationToken),
            InventoryApprovalRequestType.MovementReversal =>
                ApproveReversalAsync(approval, userId, note, now, cancellationToken),
            InventoryApprovalRequestType.DamagedDisposal =>
                ApproveDisposalAsync(approval, userId, note, now, cancellationToken),
            _ => Task.FromResult(Result.Failure(
                "نوع طلب الاعتماد غير مدعوم بعد.", "ApprovalRequired"))
        };

    private async Task<Result> ApproveEditAsync(
        InventoryApprovalRequest approval,
        int userId,
        string note,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (approval.OrderId is not int orderId || approval.TargetVersion is not int targetVersion)
            return MarkStale(approval, "طلب التعديل لا يشير إلى طلب صالح.", note, now);

        Order order = await orders.GetForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return MarkStale(approval, "الطلب المرتبط لم يعد موجودًا؛ أُغلق طلب التعديل.", note, now);

        if (!order.CanEdit || order.Version != targetVersion)
            return MarkStale(approval, "تغيّر الطلب بعد إرسال التعديل؛ أُغلق الطلب وأعد الإرسال.", note, now);

        OrderInput.Order payload = OrderEditPayload.Deserialize(approval.Payload);
        if (payload is null)
            return MarkStale(approval, "تعذر قراءة حمولة التعديل؛ أُغلق الطلب وأعد الإرسال.", note, now);

        Result<OrderUpdateReceipt> applied = await OrderEditApplier.ApplyAsync(
            order,
            payload,
            preserveIssued: true,
            manageTransaction: false,
            orders,
            inventory,
            warehouses,
            issueOrders,
            unitOfWork,
            now,
            cancellationToken);
        if (!applied.IsSuccess)
            return Result.Failure(applied.ErrorMessage, applied.ErrorCode);

        return approval.Approve(userId, note, now);
    }

    private async Task<Result> ApproveCancelAsync(
        InventoryApprovalRequest approval,
        int userId,
        string note,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (approval.StockIssueOrderId is not int issueOrderId)
            return MarkStale(approval, "طلب الإلغاء لا يشير إلى أمر صرف صالح.", note, now);

        StockIssueOrder issueOrder = await issueOrders.GetForUpdateAsync(issueOrderId, cancellationToken);
        if (issueOrder is null)
            return MarkStale(approval, "أمر الصرف لم يعد موجودًا؛ أُغلق طلب الإلغاء.", note, now);

        if (issueOrder.Status != WorkRecordStatus.CancelRequested)
            return MarkStale(approval, "تغيّرت حالة أمر الصرف بعد إرسال الطلب؛ أُغلق طلب الإلغاء.", note, now);

        if (approval.TargetVersion is int targetVersion && issueOrder.Version != targetVersion)
            return MarkStale(approval, "تغيّر أمر الصرف بعد إرسال الطلب؛ أُغلق طلب الإلغاء.", note, now);

        await inventory.LockBalancesAsync(
            issueOrder.Lines
                .Select(line => (issueOrder.WarehouseId, line.ProductId))
                .Distinct()
                .ToList(),
            cancellationToken);

        foreach (StockIssueLine line in issueOrder.Lines)
        {
            int releaseQty = Math.Min(line.RemainingQty, line.Allocation.Remaining);
            if (releaseQty <= 0)
                continue;

            if (!await inventory.ReleaseAsync(
                    issueOrder.WarehouseId,
                    line.ProductId,
                    releaseQty,
                    cancellationToken))
            {
                return Result.Failure(
                    "تعذر تحرير الكمية المحجوزة؛ حدّث الرصيد ثم أعد المحاولة.",
                    "Conflict");
            }

            Result shrunk = line.Allocation.ChangeQuantity(line.Allocation.Qty - releaseQty);
            if (!shrunk.IsSuccess)
                return shrunk;
        }

        Result cancelled = issueOrder.Cancel(approval.Reason, now);
        if (!cancelled.IsSuccess)
            return cancelled;

        return approval.Approve(userId, note, now);
    }

    private async Task<Result> ApproveReversalAsync(
        InventoryApprovalRequest approval,
        int userId,
        string note,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (approval.InventoryMovementId is not int movementId)
            return MarkStale(approval, "طلب العكس لا يشير إلى حركة صالحة.", note, now);

        InventoryMovement movement = await inventory.GetMovementForUpdateAsync(movementId, cancellationToken);
        if (movement is null)
            return MarkStale(approval, "الحركة لم تعد موجودة أمام المخزون.", note, now);

        if (movement.Status != InventoryMovementStatus.Active)
            return MarkStale(approval, "الحركة عُكست مسبقًا أو لم تعد نشطة.", note, now);

        await inventory.LockBalancesAsync(
            [(movement.WarehouseId, movement.ProductId)],
            cancellationToken);

        StockBalance balance = await inventory.GetOrCreateStockItemAsync(
            movement.WarehouseId,
            movement.ProductId,
            cancellationToken);

        int quantity = movement.Quantity;
        if (movement.MovementType == InventoryMovementType.IssueOut)
        {
            Result batchReversed = await ReverseIssueBatchAsync(
                movement,
                balance,
                now,
                cancellationToken);
            if (!batchReversed.IsSuccess)
                return batchReversed;
        }
        else if (movement.MovementType == InventoryMovementType.DamagedDisposal)
        {
            balance.DamagedIn(quantity);
        }
        else if (movement.Direction == InventoryDirection.Out)
        {
            balance.ApplyIn(quantity);
        }
        else
        {
            balance.ApplyOut(quantity);
        }

        inventory.Add(InventoryMovement.Create(
            movement.WarehouseId,
            movement.ProductId,
            quantity,
            movement.Direction == InventoryDirection.In ? InventoryDirection.Out : InventoryDirection.In,
            InventoryMovementType.Reversal,
            now,
            userId,
            note: approval.Reason,
            reversalOfMovementId: movement.Id,
            nowUtc: now));

        Result marked = movement.MarkReversed();
        if (!marked.IsSuccess)
            return marked;

        return approval.Approve(userId, note, now);
    }

    private async Task<Result> ApproveDisposalAsync(
        InventoryApprovalRequest approval,
        int userId,
        string note,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (approval.WarehouseId is not int warehouseId ||
            approval.ProductId is not int productId ||
            approval.DisposalQuantity is not int quantity ||
            quantity <= 0)
            return MarkStale(approval, "طلب الإتلاف لا يشير إلى رصيد أو كمية صالحة.", note, now);

        await inventory.LockBalancesAsync([(warehouseId, productId)], cancellationToken);

        StockBalance balance = await inventory.GetStockItemAsync(warehouseId, productId, cancellationToken);
        if (balance is null)
            return MarkStale(approval, "لا يوجد رصيد لهذا الصنف في المخزن؛ أُغلق طلب الإتلاف.", note, now);

        if (approval.TargetVersion is int targetVersion && balance.Version != targetVersion)
            return MarkStale(approval, "تغيّر رصيد المخزون بعد إرسال الطلب؛ أُغلق طلب الإتلاف وأعد الإرسال.", note, now);

        if (quantity > balance.DamagedQty)
            return MarkStale(approval, "الكمية التالفة الحالية أقل من كمية الإتلاف المطلوبة؛ أُغلق الطلب وأعد الإرسال.", note, now);

        balance.DamagedDisposal(quantity);

        inventory.Add(InventoryMovement.Create(
            warehouseId,
            productId,
            quantity,
            InventoryDirection.Out,
            InventoryMovementType.DamagedDisposal,
            now,
            userId,
            note: approval.Reason,
            nowUtc: now));

        return approval.Approve(userId, note, now);
    }

    private async Task<Result> ReverseIssueBatchAsync(
        InventoryMovement movement,
        StockBalance balance,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (movement.SourceIssueBatchId is not int batchId)
            return Result.Failure("لا يمكن عكس صرف بلا دفعة مرتبطة.", "Conflict");

        StockIssueOrder issueOrder = await issueOrders.GetForBatchAsync(batchId, cancellationToken);
        StockIssueLine line = issueOrder?.Lines
            .FirstOrDefault(candidate => candidate.Batches.Any(batch => batch.Id == batchId));
        StockIssueBatch batch = line?.Batches.FirstOrDefault(candidate => candidate.Id == batchId);
        if (issueOrder is null || line is null || batch is null)
            return Result.Failure("دفعة الصرف المرتبطة لم تعد موجودة.", "Conflict");

        // Restores the physical quantity and re-reserves it for the allocation, then rolls
        // the issued accounting back so the issue order reopens for execution.
        balance.ApplyIn(batch.Quantity);
        balance.Reserve(batch.Quantity);

        Result rolledBack = line.Allocation.RollBackIssue(batch.Quantity);
        if (!rolledBack.IsSuccess)
            return rolledBack;

        line.RemoveBatch(batch);
        issueOrders.RemoveBatch(batch);

        return issueOrder.ReopenAfterBatchRemoval(now);
    }

    private async Task<Result> RejectAsync(
        InventoryApprovalRequest approval,
        int userId,
        string note,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (approval.RequestType == InventoryApprovalRequestType.IssueOrderCancel &&
            approval.StockIssueOrderId is int issueOrderId)
        {
            StockIssueOrder issueOrder = await issueOrders.GetForUpdateAsync(issueOrderId, cancellationToken);
            if (issueOrder is null)
                return MarkStale(approval, "أمر الصرف لم يعد موجودًا؛ أُغلق طلب الإلغاء.", note, now);

            if (issueOrder.Status != WorkRecordStatus.CancelRequested)
                return MarkStale(approval, "تغيّرت حالة أمر الصرف قبل رفض الإلغاء.", note, now);

            Result restored = issueOrder.RejectCancellation(note ?? approval.Reason, now);
            if (!restored.IsSuccess)
                return restored;
        }

        return approval.Reject(userId, note, now);
    }

    private static Result MarkStale(
        InventoryApprovalRequest approval,
        string message,
        string note,
        DateTime now)
    {
        Result stale = approval.MarkStale(note, now);
        return stale.IsSuccess ? Result.Failure(message, StaleCode) : stale;
    }
}
