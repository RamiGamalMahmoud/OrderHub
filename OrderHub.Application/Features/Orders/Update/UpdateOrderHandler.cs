using MediatR;
using OrderHub.Application.Features.Orders.Allocations;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Domain.Enums;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Update;

internal class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, Result<OrderUpdateReceipt>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public UpdateOrderHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IInventoryRepository inventoryRepository,
        IWarehouseRepository warehouseRepository)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _inventoryRepository = inventoryRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<OrderUpdateReceipt>> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetForUpdateAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<OrderUpdateReceipt>.Failure("الطلب غير موجود.");
        var version = order.CheckVersion(request.ExpectedVersion);
        if (!version.IsSuccess) return Result<OrderUpdateReceipt>.Failure(version.ErrorMessage, "Conflict");
        if (!order.CanEdit) return Result<OrderUpdateReceipt>.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        if (request.Order is null)
            return Result<OrderUpdateReceipt>.Failure("بيانات الطلب غير موجودة.");

        var input = request.Order with
        {
            DeliverySteps = request.Order.DeliverySteps?.ToList(),
            OrderItems = request.Order.OrderItems?.ToList()
        };

        var header = ValidateHeader(input);
        if (!header.IsSuccess)
            return Result<OrderUpdateReceipt>.Failure(header.ErrorMessage);

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
            _warehouseRepository,
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
        var addedAllocations = new List<(int PlannedIndex, int ProductId, PlannedAllocation Planned)>();
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

                if (plannedAllocation.Qty < current.IssuedQty)
                    return Result<OrderUpdateReceipt>.Failure(
                        $"{plannedItem.Input.ProductName}: لا يمكن تقليل التخصيص عن الكمية المصروفة فعليًا.");

                var delta = plannedAllocation.Qty - current.Qty;
                if (delta > 0 && current.SourceKind == AllocationSourceKind.Warehouse)
                    reservations.Add((plannedItem.Input.ProductId, current.WarehouseId!.Value, delta));
                else if (delta < 0 && current.SourceKind == AllocationSourceKind.Warehouse)
                    releases.Add((plannedItem.Input.ProductId, current.WarehouseId!.Value, -delta));

                quantityChanges.Add((current, plannedAllocation.Qty));
            }

            foreach (var current in existing.Where(allocation =>
                         !plannedKeys.Contains(AllocationKey.Create(allocation))))
            {
                if (current.IssuedQty > 0)
                    return Result<OrderUpdateReceipt>.Failure(
                        $"{plannedItem.Input.ProductName}: لا يمكن إزالة مصدر صُرفت منه كميات. راجع أوامر الصرف أولاً.");

                if (current.SourceKind == AllocationSourceKind.Warehouse && current.Remaining > 0)
                    releases.Add((plannedItem.Input.ProductId, current.WarehouseId!.Value, current.Remaining));
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
        var scratch = new Order(input.ClientId, order.OrderNumber);
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
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionActive = true;

            var balanceKeys = reservations.Concat(releases)
                .Select(change => (change.WarehouseId, change.ProductId))
                .Distinct()
                .ToList();
            if (balanceKeys.Count > 0)
                await _inventoryRepository.LockBalancesAsync(balanceKeys, cancellationToken);

            if (needsReordering)
                await _orderRepository.PrepareDeliveryStepReorderingAsync(order, cancellationToken);

            if (releases.Count > 0)
            {
                var released = await OrderAllocationReservation.ReleaseAsync(
                    _inventoryRepository, releases, cancellationToken);
                if (!released.IsSuccess)
                    return Result<OrderUpdateReceipt>.Failure(released.ErrorMessage, released.ErrorCode);
            }

            var reserved = await OrderAllocationReservation.ReserveAsync(
                _inventoryRepository, reservations, warehouses.Value, cancellationToken);
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
                _orderRepository.RemoveAllocations(droppedAllocations);
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            transactionActive = false;
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }

    private static Result ValidateHeader(OrderInput.Order input)
    {
        if (!Enum.IsDefined(input.DeliveryMethod) || !Enum.IsDefined(input.OrderSource))
            return Result.Failure("مصدر الطلب أو طريقة التوصيل غير صالح.");
        if (input.DeliveryMethod == DeliveryMethod.DeliveryMan && !input.DeliveryManId.HasValue ||
            input.DeliveryMethod == DeliveryMethod.ShippingCompany && !input.ShippingCarrierId.HasValue)
            return Result.Failure("يجب اختيار جهة التوصيل.");

        var steps = input.DeliveryMethod == DeliveryMethod.DeliveryChain
            ? (input.DeliverySteps ?? Enumerable.Empty<OrderInput.DeliveryStep>()).ToList()
            : new List<OrderInput.DeliveryStep>();
        if (steps.Any(step => step is null ||
                step.DeliveryMethod is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany) ||
                step.StepOrder <= 0 || step.HandlerId <= 0) ||
            steps.Select(step => step.StepOrder).Distinct().Count() != steps.Count)
            return Result.Failure("خطوات التوصيل أو ترتيبها غير صالح.");

        if ((input.OrderItems ?? Enumerable.Empty<OrderInput.Item>()).Any(item => item is null ||
                (item.Properties ?? Enumerable.Empty<OrderInput.Property>()).Any(property => property is null)))
            return Result.Failure("بنود الطلب أو خصائصها غير صالحة.");

        return Result.Success();
    }

    private static OrderDeliveryStep BuildDeliveryStep(OrderInput.DeliveryStep step) => new()
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
