using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrders;

internal sealed class GetIssueOrdersQueryHandler(
    IStockIssueOrderRepository issueOrders,
    IWarehouseRepository warehouses,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetIssueOrdersQuery, Result<StockIssueOrderList>>
{
    public async Task<Result<StockIssueOrderList>> Handle(
        GetIssueOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<StockIssueOrderList>.Failure("يجب تسجيل الدخول لعرض أوامر الصرف.", "Unauthorized");

        Warehouse keeperWarehouse = await warehouses.GetByKeeperAsync(userId, cancellationToken);
        bool canApprove = await authorization.IsAllowedAsync(
            PermissionFeature.Inventory,
            PermissionLevel.Approve,
            cancellationToken);
        bool canCreate = await authorization.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.Write,
            cancellationToken);
        bool canRecord = keeperWarehouse is not null &&
            await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Write,
                cancellationToken);

        IReadOnlyList<StockIssueOrder> orders;
        string scopeLabel;

        if (keeperWarehouse is not null)
        {
            orders = await issueOrders.GetScopedAsync(keeperWarehouse.Id, null, cancellationToken);
            scopeLabel = $"أوامر الصرف الواردة إلى {keeperWarehouse.Name}";
        }
        else if (canApprove)
        {
            orders = await issueOrders.GetScopedAsync(null, null, cancellationToken);
            scopeLabel = "كل أوامر الصرف";
        }
        else
        {
            orders = await issueOrders.GetScopedAsync(null, userId, cancellationToken);
            scopeLabel = "أوامر الصرف التي أنشأتها";
        }

        var items = orders
            .OrderBy(order => StatusRank(order.Status))
            .ThenByDescending(order => order.CreatedAt)
            .Select(StockIssueOrderMapper.ToSummary)
            .ToList();

        return Result<StockIssueOrderList>.Success(new StockIssueOrderList(
            canCreate,
            canRecord,
            keeperWarehouse is not null,
            scopeLabel,
            items));
    }

    private static int StatusRank(WorkRecordStatus status) => status switch
    {
        WorkRecordStatus.Pending => 0,
        WorkRecordStatus.PartiallyExecuted => 1,
        WorkRecordStatus.CancelRequested => 2,
        WorkRecordStatus.Completed => 3,
        WorkRecordStatus.Cancelled => 4,
        _ => 5
    };
}
