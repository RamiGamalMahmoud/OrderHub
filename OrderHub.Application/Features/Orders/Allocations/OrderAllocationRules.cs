using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Application.Features.Orders.Allocations;

internal sealed record PlannedAllocation(
    AllocationSourceKind SourceKind,
    int Qty,
    int? WarehouseId,
    int? SupplierId);

internal sealed record PlannedItem(
    OrderInput.Item Input,
    IReadOnlyList<PlannedAllocation> Allocations,
    int? SupplierId,
    string SupplierName);

/// <summary>Editor-payload validation for the D17 source rule and item quantities.</summary>
internal static class OrderAllocationRules
{
    public const string MissingSourceMessage = "يجب تحديد مصدر البند (مخزن أو مورد) قبل إضافته للطلب.";

    public static Result<IReadOnlyList<PlannedItem>> Plan(
        IReadOnlyList<OrderInput.Item> items,
        bool allowExistingIds)
    {
        var planned = new List<PlannedItem>();
        foreach (var item in items)
        {
            if (item is null)
                return Result<IReadOnlyList<PlannedItem>>.Failure("بنود الطلب غير صالحة.");

            if (item.ItemId.HasValue && !allowExistingIds)
                return Result<IReadOnlyList<PlannedItem>>.Failure(
                    $"{item.ProductName}: معرف البند غير صالح لطلب جديد.");

            var allocations = (item.Allocations ?? Enumerable.Empty<OrderInput.Allocation>()).ToList();
            if (allocations.Count == 0 || allocations.Any(allocation => allocation is null))
                return Result<IReadOnlyList<PlannedItem>>.Failure($"{item.ProductName}: {MissingSourceMessage}");

            if (item.Quantity <= 0)
                return Result<IReadOnlyList<PlannedItem>>.Failure($"{item.ProductName}: يجب أن تكون الكمية أكبر من صفر.");

            var plannedAllocations = new List<PlannedAllocation>();
            var warehouses = new HashSet<int>();
            int? supplierId = null;
            foreach (var allocation in allocations)
            {
                if (allocation.Qty <= 0)
                    return Result<IReadOnlyList<PlannedItem>>.Failure(
                        $"{item.ProductName}: كمية المصدر يجب أن تكون أكبر من صفر.");

                if (allocation.SourceKind == AllocationSourceKind.Warehouse)
                {
                    if (allocation.WarehouseId is not > 0)
                        return Result<IReadOnlyList<PlannedItem>>.Failure(
                            $"{item.ProductName}: يجب تحديد المخزن لتخصيص المخزون.");

                    if (!warehouses.Add(allocation.WarehouseId.Value))
                        return Result<IReadOnlyList<PlannedItem>>.Failure(
                            $"{item.ProductName}: لا يمكن تكرار المخزن نفسه في تخصيصات البند.");

                    plannedAllocations.Add(new PlannedAllocation(
                        AllocationSourceKind.Warehouse,
                        allocation.Qty,
                        allocation.WarehouseId,
                        null));
                    continue;
                }

                if (allocation.SourceKind != AllocationSourceKind.Supplier)
                    return Result<IReadOnlyList<PlannedItem>>.Failure($"{item.ProductName}: مصدر التخصيص غير صالح.");

                if (allocation.SupplierId is not > 0)
                    return Result<IReadOnlyList<PlannedItem>>.Failure(
                        $"{item.ProductName}: يجب تحديد المورد لتخصيص التوريد.");

                if (supplierId.HasValue)
                    return Result<IReadOnlyList<PlannedItem>>.Failure(
                        $"{item.ProductName}: لا يمكن تخصيص أكثر من كمية واحدة للمورد.");

                supplierId = allocation.SupplierId;
                plannedAllocations.Add(new PlannedAllocation(
                    AllocationSourceKind.Supplier,
                    allocation.Qty,
                    null,
                    allocation.SupplierId));
            }

            if (plannedAllocations.Sum(allocation => allocation.Qty) != item.Quantity)
                return Result<IReadOnlyList<PlannedItem>>.Failure(
                    $"{item.ProductName}: يجب أن يساوي مجموع كميات المصادر كمية البند.");

            if (supplierId.HasValue)
            {
                if (item.SupplierId.HasValue && item.SupplierId != supplierId)
                    return Result<IReadOnlyList<PlannedItem>>.Failure(
                        $"{item.ProductName}: المورد المحدد لا يطابق تخصيص التوريد.");
            }

            planned.Add(new PlannedItem(
                item,
                plannedAllocations,
                supplierId,
                supplierId.HasValue ? item.SupplierName : null));
        }

        if (planned.Count == 0)
            return Result<IReadOnlyList<PlannedItem>>.Failure("يجب إضافة منتج واحد على الأقل.");

        return Result<IReadOnlyList<PlannedItem>>.Success(planned);
    }
}
