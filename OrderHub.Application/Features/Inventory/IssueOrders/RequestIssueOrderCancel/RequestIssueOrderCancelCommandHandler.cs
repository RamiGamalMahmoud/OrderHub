using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.IssueOrders.RequestIssueOrderCancel;

internal sealed class RequestIssueOrderCancelCommandHandler(
    IStockIssueOrderRepository issueOrders,
    IInventoryApprovalRepository approvals,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<RequestIssueOrderCancelCommand, Result>
{
    public async Task<Result> Handle(
        RequestIssueOrderCancelCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result.Failure("يجب تسجيل الدخول لطلب الإلغاء.", "Unauthorized");

        StockIssueOrder issueOrder = await issueOrders.GetForUpdateAsync(
            request.IssueOrderId,
            cancellationToken);
        if (issueOrder is null)
            return Result.Failure("أمر الصرف غير موجود.", "NotFound");

        if (issueOrder.Warehouse.KeeperUserId != userId)
        {
            return Result.Failure(
                "طلب الإلغاء متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;

        Result requested;
        try
        {
            requested = issueOrder.RequestCancellation(userId, request.Reason, now);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure(exception.Message);
        }

        if (!requested.IsSuccess)
            return requested;

        Result<InventoryApprovalRequest> approval = InventoryApprovalRequest.CreateIssueOrderCancel(
            issueOrder.Id,
            issueOrder.Version,
            issueOrder.CancelReason,
            userId,
            now);
        if (!approval.IsSuccess)
            return Result.Failure(approval.ErrorMessage);

        approvals.Add(approval.Value);

        var transactionActive = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            transactionActive = false;
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            return Result.Failure("يوجد طلب إلغاء قيد الاعتماد لهذا الأمر.", "Conflict");
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ طلب الإلغاء. يرجى المحاولة مرة أخرى.", "Persistence");
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }

        return Result.Success();
    }
}
