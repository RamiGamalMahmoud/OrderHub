using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.WarehouseStock;

/// <summary>
/// Resolves which warehouses a caller may read: Admin / Inventory.Approve sees every warehouse,
/// a warehouse keeper sees his own, any other Inventory.Read holder sees every warehouse.
/// </summary>
public sealed record InventoryScope(bool IsUnrestricted, int KeeperWarehouseId, string KeeperWarehouseName)
{
    public bool IsKeeper => KeeperWarehouseId > 0 && !IsUnrestricted;

    public bool Includes(int warehouseId) => IsUnrestricted || KeeperWarehouseId == warehouseId;

    public string ScopeLabel => IsUnrestricted
        ? "كل المخازن"
        : $"مخزن {KeeperWarehouseName}";

    public static async Task<Result<InventoryScope>> ResolveAsync(
        ICurrentUser currentUser,
        IAuthorizationService authorization,
        IWarehouseRepository warehouses,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.Id is not int userId || userId <= 0)
        {
            return Result<InventoryScope>.Failure(
                "يجب تسجيل الدخول لعرض أرصدة المخزون.",
                "Unauthorized");
        }

        bool canApprove = await authorization.IsAllowedAsync(
            PermissionFeature.Inventory,
            PermissionLevel.Approve,
            cancellationToken);
        if (canApprove)
            return Result<InventoryScope>.Success(new InventoryScope(true, 0, null));

        Warehouse keeperWarehouse = await warehouses.GetByKeeperAsync(userId, cancellationToken);
        if (keeperWarehouse is not null)
        {
            return Result<InventoryScope>.Success(new InventoryScope(
                false,
                keeperWarehouse.Id,
                keeperWarehouse.Name?.Value));
        }

        bool canRead = await authorization.IsAllowedAsync(
            PermissionFeature.Inventory,
            PermissionLevel.Read,
            cancellationToken);
        if (canRead)
            return Result<InventoryScope>.Success(new InventoryScope(true, 0, null));

        return Result<InventoryScope>.Failure(
            "لا تملك صلاحية عرض أرصدة المخزون.",
            "Forbidden");
    }

    public static async Task<Result<InventoryWarehouseHeaderRow>> ResolveWarehouseAsync(
        InventoryScope scope,
        IWarehouseRepository warehouses,
        int warehouseId,
        CancellationToken cancellationToken)
    {
        InventoryWarehouseHeaderRow header = await warehouses
            .GetInventoryHeaderAsync(warehouseId, cancellationToken);
        if (header is null)
            return Result<InventoryWarehouseHeaderRow>.Failure("المخزن غير موجود.", "NotFound");

        if (!scope.Includes(header.Id))
        {
            return Result<InventoryWarehouseHeaderRow>.Failure(
                "لا تملك صلاحية الوصول إلى هذا المخزن.",
                "Forbidden");
        }

        return Result<InventoryWarehouseHeaderRow>.Success(header);
    }
}
