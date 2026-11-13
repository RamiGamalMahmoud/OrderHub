using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrderDetails;

internal sealed class GetIssueOrderDetailsQueryHandler(
    IStockIssueOrderRepository issueOrders,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetIssueOrderDetailsQuery, Result<StockIssueOrderDetails>>
{
    public async Task<Result<StockIssueOrderDetails>> Handle(
        GetIssueOrderDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<StockIssueOrderDetails>.Failure("يجب تسجيل الدخول لعرض أمر الصرف.", "Unauthorized");

        StockIssueOrder issueOrder = await issueOrders.GetDetailsAsync(
            request.IssueOrderId,
            cancellationToken);
        if (issueOrder is null)
            return Result<StockIssueOrderDetails>.Failure("أمر الصرف غير موجود.", "NotFound");

        bool canApprove = await authorization.IsAllowedAsync(
            PermissionFeature.Inventory,
            PermissionLevel.Approve,
            cancellationToken);

        if (!canApprove &&
            issueOrder.RequestedByUserId != userId &&
            issueOrder.Warehouse.KeeperUserId != userId)
        {
            return Result<StockIssueOrderDetails>.Failure(
                "لا تملك صلاحية عرض هذا الأمر.",
                "Forbidden");
        }

        bool isKeeper = issueOrder.Warehouse.KeeperUserId == userId;
        bool canRecord = isKeeper && issueOrder.AcceptsBatches &&
            await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Write,
                cancellationToken);
        bool canRequestCancel = isKeeper &&
            issueOrder.Status is WorkRecordStatus.Pending or WorkRecordStatus.PartiallyExecuted;

        return Result<StockIssueOrderDetails>.Success(StockIssueOrderMapper.ToDetails(issueOrder) with
        {
            CanRecordBatches = canRecord,
            CanRequestCancel = canRequestCancel
        });
    }
}
