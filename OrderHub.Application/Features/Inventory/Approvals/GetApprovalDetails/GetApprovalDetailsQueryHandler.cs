using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Approvals.Contracts;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Features.Warehouses.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Approvals.GetApprovalDetails;

internal sealed class GetApprovalDetailsQueryHandler(
    IInventoryApprovalRepository approvals,
    IOrderRepository orders,
    IWarehouseRepository warehouses,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetApprovalDetailsQuery, Result<ApprovalDetailsResult>>
{
    public async Task<Result<ApprovalDetailsResult>> Handle(
        GetApprovalDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<ApprovalDetailsResult>.Failure(
                "يجب تسجيل الدخول لعرض تفاصيل الاعتماد.",
                "Unauthorized");

        if (!await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Approve,
                cancellationToken))
        {
            return Result<ApprovalDetailsResult>.Failure(
                "عرض تفاصيل الاعتماد متاح للمدير المختص.",
                "Forbidden");
        }

        InventoryApprovalProjectionRow row = await approvals.GetRowAsync(
            request.ApprovalId,
            cancellationToken);
        if (row is null)
            return Result<ApprovalDetailsResult>.Failure("طلب الاعتماد غير موجود.", "NotFound");

        if (row.RequestType != InventoryApprovalRequestType.AllocationEdit || row.OrderId is null)
            return Result<ApprovalDetailsResult>.Success(
                new ApprovalDetailsResult(row, [], []));

        var names = (await warehouses.GetListAsync(cancellationToken))
            .ToDictionary(warehouse => warehouse.Id, warehouse => warehouse.Name);
        Order order = await orders.GetForUpdateAsync(row.OrderId.Value, cancellationToken);
        if (order is null)
            return Result<ApprovalDetailsResult>.Success(
                new ApprovalDetailsResult(row, [], []));

        InventoryApprovalRequest payloadOwner = await approvals.GetForUpdateAsync(
            request.ApprovalId,
            cancellationToken);
        OrderInput.Order proposed = OrderEditPayload.Deserialize(payloadOwner?.Payload);

        var proposedItems = (proposed?.OrderItems ?? [])
            .Select(item => new ApprovalItemView(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                (item.Allocations ?? [])
                    .Select(allocation => new ApprovalAllocationView(
                        allocation.SourceKind,
                        allocation.WarehouseId,
                        allocation.WarehouseId is int warehouseId && names.TryGetValue(warehouseId, out string name)
                            ? name
                            : null,
                        allocation.SourceKind == AllocationSourceKind.Supplier ? item.SupplierName : null,
                        allocation.Qty))
                    .ToList()))
            .ToList();

        var currentItems = order.OrderItems
            .Select(item => new ApprovalItemView(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                order.Allocations
                    .Where(allocation => allocation.OrderItemId == item.Id &&
                        allocation.Status == OrderAllocationStatus.Active)
                    .Select(allocation => new ApprovalAllocationView(
                        allocation.SourceKind,
                        allocation.WarehouseId,
                        allocation.WarehouseId is int warehouseId && names.TryGetValue(warehouseId, out string name)
                            ? name
                            : null,
                        allocation.SourceKind == AllocationSourceKind.Supplier ? item.SupplierName : null,
                        allocation.Qty))
                    .ToList()))
            .ToList();

        return Result<ApprovalDetailsResult>.Success(
            new ApprovalDetailsResult(row, proposedItems, currentItems));
    }
}
