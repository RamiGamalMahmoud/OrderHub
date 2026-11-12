using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Allocations;

internal static class OrderAllocationReservation
{
    public const string ConflictMessage = "الكمية لم تعد متاحة في {0}؛ أعد التوزيع.";

    /// <summary>Validates that every referenced warehouse exists and is active.</summary>
    public static async Task<Result<IReadOnlyDictionary<int, string>>> LoadWarehousesAsync(
        IWarehouseRepository warehouses,
        IEnumerable<int> warehouseIds,
        CancellationToken cancellationToken)
    {
        var wanted = warehouseIds.Distinct().ToList();
        var map = new Dictionary<int, string>();
        if (wanted.Count == 0)
            return Result<IReadOnlyDictionary<int, string>>.Success(map);

        var list = await warehouses.GetListAsync(cancellationToken);
        foreach (var id in wanted)
        {
            var warehouse = list.FirstOrDefault(item => item.Id == id);
            if (warehouse is null)
                return Result<IReadOnlyDictionary<int, string>>.Failure("المخزن المحدد غير موجود.");
            if (!warehouse.IsActive)
                return Result<IReadOnlyDictionary<int, string>>.Failure(
                    $"المخزن {warehouse.Name} غير نشط ولا يمكن التخصيص منه.");
            map[id] = warehouse.Name;
        }

        return Result<IReadOnlyDictionary<int, string>>.Success(map);
    }

    /// <summary>Reserves ascending (product, warehouse) as required by D13.</summary>
    public static async Task<Result> ReserveAsync(
        IInventoryRepository inventory,
        IReadOnlyCollection<(int ProductId, int WarehouseId, int Qty)> reservations,
        IReadOnlyDictionary<int, string> warehouseNames,
        CancellationToken cancellationToken)
    {
        foreach (var reservation in reservations
                     .OrderBy(item => item.ProductId)
                     .ThenBy(item => item.WarehouseId))
        {
            if (await inventory.TryReserveAsync(
                    reservation.WarehouseId,
                    reservation.ProductId,
                    reservation.Qty,
                    cancellationToken))
                continue;

            var name = warehouseNames.TryGetValue(reservation.WarehouseId, out var warehouseName)
                ? warehouseName
                : "المخزن المحدد";
            return Result.Failure(string.Format(ConflictMessage, name), "Conflict");
        }

        return Result.Success();
    }

    /// <summary>Releases ascending (product, warehouse) inside the same transaction.</summary>
    public static async Task<Result> ReleaseAsync(
        IInventoryRepository inventory,
        IReadOnlyCollection<(int ProductId, int WarehouseId, int Qty)> releases,
        CancellationToken cancellationToken)
    {
        foreach (var release in releases
                     .OrderBy(item => item.ProductId)
                     .ThenBy(item => item.WarehouseId))
        {
            if (!await inventory.ReleaseAsync(
                    release.WarehouseId,
                    release.ProductId,
                    release.Qty,
                    cancellationToken))
                return Result.Failure("تعذر تحرير الكمية المحجوزة. حدّث الطلب ثم أعد المحاولة.", "Conflict");
        }

        return Result.Success();
    }

    public static IEnumerable<(int ProductId, int WarehouseId, int Qty)> WarehouseReservations(
        int productId,
        IEnumerable<PlannedAllocation> allocations) =>
        allocations
            .Where(allocation => allocation.SourceKind == AllocationSourceKind.Warehouse)
            .Select(allocation => (ProductId: productId, WarehouseId: allocation.WarehouseId.Value, Qty: allocation.Qty));
}
