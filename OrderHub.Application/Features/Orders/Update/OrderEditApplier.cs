using OrderHub.Application.Features.Orders.Allocations;
using OrderHub.Application.Features.Orders.Contracts;
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

namespace OrderHub.Application.Features.Orders.Update;

/// <summary>
/// Applies an editor payload to a loaded order. <paramref name="preserveIssued"/> keeps
/// already-issued quantities after an approved edit and records the excess as a pending return.
/// </summary>
internal static class OrderEditApplier
{
    public const string PendingEditMessage =
        "يوجد تعديل قيد الاعتماد لهذا الطلب؛ انتظر قرار المدير أو أعد الإرسال بعد ردّه.";

    public static async Task<Result<OrderUpdateReceipt>> ApplyAsync(
        Order order,
        OrderInput.Order input,
        bool preserveIssued,
        bool manageTransaction,
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IWarehouseRepository warehouseRepository,
        IStockIssueOrderRepository issueOrderRepository,
        IUnitOfWork unitOfWork,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var steps = input.DeliveryMethod == DeliveryMethod.DeliveryChain
            ? (input.DeliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>()).OrderBy(step => step.StepOrder).ToList()
            : new List<OrderInput.DeliveryStep>();

        var stepIds = steps.Where(step => step.Id.HasValue).Select(step => step.Id!.Value).ToList();
        if (stepIds.Distinct().Count() != stepIds.Count ||
            stepIds.Any(id => id <= 0 || order.DeliverySteps.All(step => step.Id != id)))
            return Result<OrderUpdateReceipt>.Failure("معرفات خطوات التوصيل غير صالحة لهذا الطلب.");

        var inputItems = (input.OrderItems ?? []).ToList();
        var itemIds = inputItems.Where(item => item.ItemId.HasValue).Select(item => item.ItemId!.Value).ToList();
        if (itemIds.Distinct().Count() != itemIds.Count ||
            itemIds.Any(id => id <= 0 || order.OrderItems.All(item => item.Id != id)))
            return Result<OrderUpdateReceipt>.Failure("معرفات بنود الطلب غير صالحة لهذا الطلب.");

        var plan = OrderAllocationRules.Plan(inputItems, allowExistingIds: true);
        if (!plan.IsSuccess)
            return Result<OrderUpdateReceipt>.Failure(plan.ErrorMessage);

        var warehouses = await OrderAllocationReservation.LoadWarehousesAsync(
            warehouseRepository,
            plan.Value.SelectMany(item => item.Allocations)
                .Where(allocation => allocation.SourceKind == AllocationSourceKind.Warehouse)
                .Select(allocation => allocation.WarehouseId!.Value),
            cancellationToken);
        if (!warehouses.IsSuccess)
            return Result<OrderUpdateReceipt>.Failure(warehouses.ErrorMessage);

        var reservations = new List<(int ProductId, int WarehouseId, int Qty)>();
        var releases = new List<(int ProductId, int WarehouseId, int Qty)>();
        var droppedAllocations = new List<OrderInventoryAllocation>();
        var quantityChanges = new List<(OrderInventoryAllocation Allocation, int Qty)>();
        var pendingReturns = new List<(OrderInventoryAllocation Allocation, int Qty)>();
        var addedAllocations = new List<(int PlannedIndex, int ProductId, PlannedAllocation Planned)>();
        var affectedAllocationIds = new HashSet<int>();
        var keptItemIds = new HashSet<int>(itemIds);

        for (var index = 0; index < plan.Value.Count; index++)
        {
            var plannedItem = plan.Value[index];
            var itemId = plannedItem.Input.ItemId;
            var existing = itemId.HasValue
                ? order.Allocations
                    .Where(allocation => allocation.OrderItemId == itemId.Value &&
                        allocation.Status == OrderAllocationStatus.Active)
                    .ToList()
                : new List<OrderInventoryAllocation>();
            var plannedKeys = plannedItem.Allocations
                .Select(AllocationKey.Create)
                .ToHashSet();

            foreach (var plannedAllocation in plannedItem.Allocations)
            {
                var key = AllocationKey.Create(plannedAllocation);
                var current = existing.FirstOrDefault(allocation => AllocationKey.Create(allocation) == key);
                if (current is null)
                {
                    if (plannedAllocation.SourceKind == AllocationSourceKind.Warehouse)
                        reservations.Add((plannedItem.Input.ProductId,
                            plannedAllocation.WarehouseId!.Value, plannedAllocation.Qty));
                    addedAllocations.Add((index, plannedItem.Input.ProductId, plannedAllocation));
                    continue;
                }

                if (!preserveIssued && plannedAllocation.Qty < current.IssuedQty)
                    return Result<OrderUpdateReceipt>.Failure(
                        $"{plannedItem.Input.ProductName}: لا يمكن تقليل التخصيص عن الكمية المصروفة فعليًا.");

                var delta = plannedAllocation.Qty - current.Qty;
                if (delta > 0 && current.SourceKind == AllocationSourceKind.Warehouse)
                {
                    reservations.Add((plannedItem.Input.ProductId, current.WarehouseId!.Value, delta));
                }
                else if (delta < 0 && current.SourceKind == AllocationSourceKind.Warehouse)
                {
                    var releaseQty = Math.Min(-delta, Math.Max(0, current.Remaining));
                    if (releaseQty > 0)
                        releases.Add((plannedItem.Input.ProductId, current.WarehouseId!.Value, releaseQty));
                }

                if (preserveIssued && plannedAllocation.Qty < current.IssuedQty)
                {
                    quantityChanges.Add((current, current.IssuedQty));
                    pendingReturns.Add((current, current.IssuedQty - plannedAllocation.Qty));
                    affectedAllocationIds.Add(current.Id);
                }
                else
                {
                    quantityChanges.Add((current, plannedAllocation.Qty));
                    if (preserveIssued && delta < 0)
                        affectedAllocationIds.Add(current.Id);
                    if (preserveIssued && current.PendingReturnQty > 0)
                        pendingReturns.Add((current, 0));
                }
            }

            foreach (var current in existing.Where(allocation =>
                         !plannedKeys.Contains(AllocationKey.Create(allocation))))
            {
                if (current.IssuedQty > 0)
                {
                    if (!preserveIssued)
                        return Result<OrderUpdateReceipt>.Failure(
                            $"{plannedItem.Input.ProductName}: لا يمكن إزالة مصدر صُرفت منه كميات. راجع أوامر الصرف أولاً.");

                    // Keeps the issued quantity on its source and queues it as a pending return;
                    // the un-issued remainder is released.
                    if (current.Remaining > 0)
                        releases.Add((plannedItem.Input.ProductId, current.WarehouseId!.Value, current.Remaining));
                    quantityChanges.Add((current, current.IssuedQty));
                    pendingReturns.Add((current, current.IssuedQty));
                    affectedAllocationIds.Add(current.Id);
                    continue;
                }

                if (current.SourceKind == AllocationSourceKind.Warehouse && current.Remaining > 0)
                    releases.Add((plannedItem.Input.ProductId, current.WarehouseId!.Value, current.Remaining));
                if (preserveIssued)
                    affectedAllocationIds.Add(current.Id);
                droppedAllocations.Add(current);
            }
        }

        foreach (var item in order.OrderItems.Where(item => !keptItemIds.Contains(item.Id)))
        {
            foreach (var allocation in order.Allocations.Where(allocation =>
                         allocation.OrderItemId == item.Id &&
                         allocation.Status == OrderAllocationStatus.Active))
            {
                if (allocation.IssuedQty > 0)
                    return Result<OrderUpdateReceipt>.Failure(
                        $"{item.ProductName}: لا يمكن حذف بند صُرفت منه كميات. راجع أوامر الصرف أولاً.");
                if (allocation.SourceKind == AllocationSourceKind.Warehouse && allocation.Remaining > 0)
                    releases.Add((item.ProductId, allocation.WarehouseId!.Value, allocation.Remaining));
                droppedAllocations.Add(allocation);
            }
        }

        var values = new List<OrderItem>();
        var scratch = new Order(order.ClientId, order.OrderNumber);
        foreach (var plannedItem in plan.Value)
        {
            var created = scratch.AddOrderItem(
                plannedItem.Input.ProductId,
                plannedItem.Input.ProductName,
                plannedItem.Input.UnitPrice,
                plannedItem.Input.Quantity,
                plannedItem.Input.VatRate,
                plannedItem.SupplierName,
                plannedItem.SupplierId,
                (plannedItem.Input.Properties ?? Enumerable.Empty<OrderInput.Property>())
                    .Select(property => new OrderItemPropertyData(property.PropertyId, property.Value))
                    .ToList()
                    .AsReadOnly());
            if (!created.IsSuccess)
                return Result<OrderUpdateReceipt>.Failure(created.ErrorMessage);
            values.Add(created.Value);
        }

        var needsReordering = order.DeliverySteps.Count > 0 &&
            (order.DeliverySteps.Count != steps.Count || steps.Any(step => !step.Id.HasValue ||
                order.DeliverySteps.Single(existing => existing.Id == step.Id!.Value).StepOrder != step.StepOrder));

        var transactionActive = false;
        try
        {
            if (manageTransaction)
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                transactionActive = true;
            }

            var balanceKeys = reservations.Concat(releases)
                .Select(change => (change.WarehouseId, change.ProductId))
                .Distinct()
                .ToList();
            if (balanceKeys.Count > 0)
                await inventoryRepository.LockBalancesAsync(balanceKeys, cancellationToken);

            if (preserveIssued && affectedAllocationIds.Count > 0)
            {
                var activeIssueOrders = await issueOrderRepository.GetActiveForOrderAsync(
                    order.Id,
                    cancellationToken);
                foreach (var issueOrder in activeIssueOrders.Where(issueOrder => issueOrder.Lines
                             .Any(line => affectedAllocationIds.Contains(line.AllocationId))))
                {
                    var cancelled = issueOrder.Cancel(
                        "تعديل معتمد عدّل التخصيصات؛ أعد إنشاء أمر صرف إن لزم.",
                        nowUtc);
                    if (!cancelled.IsSuccess)
                        return Result<OrderUpdateReceipt>.Failure(cancelled.ErrorMessage, "Conflict");
                }
            }

            if (needsReordering)
                await orderRepository.PrepareDeliveryStepReorderingAsync(order, cancellationToken);

            if (releases.Count > 0)
            {
                var released = await OrderAllocationReservation.ReleaseAsync(
                    inventoryRepository, releases, cancellationToken);
                if (!released.IsSuccess)
                    return Result<OrderUpdateReceipt>.Failure(released.ErrorMessage, released.ErrorCode);
            }

            var reserved = await OrderAllocationReservation.ReserveAsync(
                inventoryRepository, reservations, warehouses.Value, cancellationToken);
            if (!reserved.IsSuccess)
                return Result<OrderUpdateReceipt>.Failure(reserved.ErrorMessage, reserved.ErrorCode);

            foreach (var allocation in droppedAllocations)
            {
                var release = allocation.Release();
                if (!release.IsSuccess)
                    return Result<OrderUpdateReceipt>.Failure(release.ErrorMessage);
            }

            // Dropped sources are removed from the aggregate and deleted so the unique
            // (item, source) index does not block re-adding the same source later.
            if (droppedAllocations.Count > 0)
            {
                orderRepository.RemoveAllocations(droppedAllocations);
                order.RemoveAllocations(droppedAllocations);
            }

            order.SynchronizeOrderItems(values
                .Zip(plan.Value, (item, planned) => (planned.Input.ItemId, item))
                .ToList());

            var itemInstances = plan.Value.Select((planned, index) => planned.Input.ItemId.HasValue
                    ? order.OrderItems.First(item => item.Id == planned.Input.ItemId.Value)
                    : values[index])
                .ToList();

            foreach (var (allocation, qty) in quantityChanges)
            {
                var changed = allocation.ChangeQuantity(qty);
                if (!changed.IsSuccess)
                    return Result<OrderUpdateReceipt>.Failure(changed.ErrorMessage);
            }

            foreach (var (allocation, qty) in pendingReturns)
            {
                var recorded = allocation.SetPendingReturn(qty);
                if (!recorded.IsSuccess)
                    return Result<OrderUpdateReceipt>.Failure(recorded.ErrorMessage);
            }

            foreach (var (plannedIndex, productId, plannedAllocation) in addedAllocations)
            {
                var created = OrderInventoryAllocation.Create(
                    productId,
                    plannedAllocation.SourceKind,
                    plannedAllocation.Qty,
                    plannedAllocation.WarehouseId,
                    plannedAllocation.SupplierId);
                if (!created.IsSuccess)
                    return Result<OrderUpdateReceipt>.Failure(created.ErrorMessage);
                order.AddAllocation(created.Value, itemInstances[plannedIndex]);
            }

            order.UpdateHeader(input.ClientId, input.OrderSource, input.DeliveryMethod,
                input.DeliveryMethod == DeliveryMethod.DeliveryMan ? input.DeliveryManId : null,
                input.DeliveryMethod == DeliveryMethod.ShippingCompany ? input.ShippingCarrierId : null,
                input.PaymentMethodId);
            order.SynchronizeDeliverySteps(steps
                .Select(step => (step.Id, BuildDeliveryStep(step)))
                .ToList());
            var financials = order.SetFinancialAdjustments(input.DeliveryCost, input.DiscountType, input.DiscountValue);
            if (!financials.IsSuccess) return Result<OrderUpdateReceipt>.Failure(financials.ErrorMessage);
            order.TouchForEdit();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            if (manageTransaction)
            {
                transactionActive = false;
                await unitOfWork.CommitTransactionAsync(cancellationToken);
            }

            return new OrderUpdateReceipt(order.Version,
                order.DeliverySteps.OrderBy(step => step.StepOrder)
                    .Select(step => new SavedDeliveryStep(step.Id, step.StepOrder)).ToList(),
                itemInstances.Select(item => new SavedOrderItem(item.Id, item.ProductId)).ToList());
        }
        catch (PersistenceConflictException exception)
        {
            return Result<OrderUpdateReceipt>.Failure(exception.Message, "Conflict");
        }
        catch (PersistenceException)
        {
            return Result<OrderUpdateReceipt>.Failure("تعذر حفظ الطلب. تحقق من البيانات المحددة ثم حاول مرة أخرى.");
        }
        catch (InvalidOperationException exception)
        {
            return Result<OrderUpdateReceipt>.Failure(exception.Message);
        }
        finally
        {
            if (transactionActive)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }

    public static OrderDeliveryStep BuildDeliveryStep(OrderInput.DeliveryStep step) => new()
    {
        StepOrder = step.StepOrder,
        DeliveryMethod = step.DeliveryMethod,
        DeliverymanId = step.DeliveryMethod == DeliveryMethod.DeliveryMan ? step.HandlerId : null,
        ShippingCarrierId = step.DeliveryMethod == DeliveryMethod.ShippingCompany ? step.HandlerId : null
    };

    private readonly record struct AllocationKey(
        AllocationSourceKind SourceKind,
        int SourceId)
    {
        public static AllocationKey Create(PlannedAllocation allocation) =>
            new(allocation.SourceKind, allocation.WarehouseId ?? allocation.SupplierId ?? 0);

        public static AllocationKey Create(OrderInventoryAllocation allocation) =>
            new(allocation.SourceKind, allocation.WarehouseId ?? allocation.SupplierId ?? 0);
    }
}
