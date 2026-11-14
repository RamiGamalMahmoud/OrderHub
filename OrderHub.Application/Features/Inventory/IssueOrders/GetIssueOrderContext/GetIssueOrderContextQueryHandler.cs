using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Inventory.IssueOrders.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.IssueOrders.GetIssueOrderContext;

internal sealed class GetIssueOrderContextQueryHandler(
    IOrderRepository orders,
    IStockIssueOrderRepository issueOrders,
    IAuthorizationService authorization) : IRequestHandler<GetIssueOrderContextQuery, Result<IssueOrderContext>>
{
    private const int OptionLimit = 100;

    public async Task<Result<IssueOrderContext>> Handle(
        GetIssueOrderContextQuery request,
        CancellationToken cancellationToken)
    {
        bool canCreate = await authorization.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.Write,
            cancellationToken);

        if (!canCreate)
            return Result<IssueOrderContext>.Failure("لا تملك صلاحية إنشاء أوامر صرف.", "Forbidden");

        IReadOnlyList<Order> openOrders = await orders.GetOrdersWithOpenWarehouseAllocationsAsync(
            OptionLimit,
            cancellationToken);

        var options = openOrders
            .Select(order => new IssueOrderOrderOption(
                order.Id,
                order.OrderNumber,
                order.Client?.Name?.Value ?? string.Empty,
                order.Allocations.Count(allocation =>
                    allocation.SourceKind == AllocationSourceKind.Warehouse &&
                    allocation.Status == OrderAllocationStatus.Active)))
            .ToList();

        IssueOrderSelectedOrder selected = null;
        if (request.OrderId is int orderId && orderId > 0)
        {
            bool isListed = options.Any(option => option.Id == orderId);
            Order order = isListed
                ? openOrders.First(option => option.Id == orderId)
                : await orders.GetForIssueOrderAsync(orderId, cancellationToken);

            if (order is null ||
                order.OrderStatus is OrderStatus.Delivered or OrderStatus.Cancelled)
                return Result<IssueOrderContext>.Failure("الطلب غير متاح لأوامر الصرف.", "NotFound");

            selected = await BuildSelectedOrderAsync(order, cancellationToken);
            if (selected.Warehouses.Count == 0)
                return Result<IssueOrderContext>.Failure(
                    "لا توجد تخصيصات مخزون نشطة متاحة لهذا الطلب.",
                    "InvalidOperation");
        }

        return Result<IssueOrderContext>.Success(new IssueOrderContext(canCreate, options, selected));
    }

    private async Task<IssueOrderSelectedOrder> BuildSelectedOrderAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var activeWarehouseAllocations = order.Allocations
            .Where(allocation =>
                allocation.SourceKind == AllocationSourceKind.Warehouse &&
                allocation.Status == OrderAllocationStatus.Active &&
                allocation.WarehouseId is > 0)
            .ToList();

        if (activeWarehouseAllocations.Count == 0)
            return new IssueOrderSelectedOrder(
                order.Id,
                order.OrderNumber,
                order.Client?.Name?.Value ?? string.Empty,
                []);

        IReadOnlyCollection<int> allocationIds = activeWarehouseAllocations
            .Select(allocation => allocation.Id)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<int, int> activeRequested =
            await issueOrders.GetActiveRequestedByAllocationAsync(allocationIds, cancellationToken);
        IReadOnlyCollection<int> blocked =
            await issueOrders.GetCancelRequestedAllocationIdsAsync(allocationIds, cancellationToken);

        var productNames = order.OrderItems
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.First().ProductName);

        var warehouses = activeWarehouseAllocations
            .Where(allocation => !blocked.Contains(allocation.Id))
            .GroupBy(allocation => allocation.WarehouseId!.Value)
            .Select(group => new
            {
                WarehouseId = group.Key,
                WarehouseName = group.First().Warehouse?.Name?.Value ?? string.Empty,
                Lines = group
                    .Select(allocation =>
                    {
                        activeRequested.TryGetValue(allocation.Id, out int requested);
                        int available = System.Math.Max(0, allocation.Remaining - requested);
                        return new IssueOrderAvailableLine(
                            allocation.Id,
                            allocation.ProductId,
                            productNames.GetValueOrDefault(allocation.ProductId) ?? string.Empty,
                            allocation.Qty,
                            allocation.IssuedQty,
                            requested,
                            available);
                    })
                    .Where(line => line.AvailableToRequest > 0)
                    .OrderBy(line => line.ProductName)
                    .ToList()
            })
            .Where(warehouse => warehouse.Lines.Count > 0)
            .OrderBy(warehouse => warehouse.WarehouseName)
            .Select(warehouse => new IssueOrderWarehouseGroup(
                warehouse.WarehouseId,
                warehouse.WarehouseName,
                warehouse.Lines))
            .ToList();

        return new IssueOrderSelectedOrder(
            order.Id,
            order.OrderNumber,
            order.Client?.Name?.Value ?? string.Empty,
            warehouses);
    }
}
