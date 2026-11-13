using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.WarehouseStock;

internal sealed class GetAccessibleWarehousesQueryHandler(
    IWarehouseRepository warehouses,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetAccessibleWarehousesQuery, Result<AccessibleWarehousesResult>>
{
    public async Task<Result<AccessibleWarehousesResult>> Handle(
        GetAccessibleWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        Result<InventoryScope> scopeResult = await InventoryScope.ResolveAsync(
            currentUser,
            authorization,
            warehouses,
            cancellationToken);
        if (!scopeResult.IsSuccess)
            return Result<AccessibleWarehousesResult>.Failure(scopeResult.ErrorMessage, scopeResult.ErrorCode);

        InventoryScope scope = scopeResult.Value;
        IReadOnlyCollection<int> scopeIds = scope.IsUnrestricted
            ? null
            : [scope.KeeperWarehouseId];
        IReadOnlyList<InventoryWarehouseSummaryRow> summaries = await warehouses
            .GetInventorySummariesAsync(scopeIds, cancellationToken);

        bool canWrite = await authorization.IsAllowedAsync(
            PermissionFeature.Inventory,
            PermissionLevel.Write,
            cancellationToken);
        bool canRecordStockIn = scope.IsKeeper && canWrite && summaries.Any(item => item.IsActive);

        var items = summaries
            .Select(row => new AccessibleWarehouseItem(
                row.Id,
                row.Name,
                row.KeeperDisplayName,
                row.IsActive,
                InventoryPresentation.BuildLocation(row.CityName, row.Street),
                row.PhoneNumber,
                row.StockOnHand,
                row.ReservedOnHand,
                row.DamagedQty,
                row.ProductCount,
                row.WithStockCount,
                row.LastMovementAtUtc))
            .ToList();

        return Result<AccessibleWarehousesResult>.Success(new AccessibleWarehousesResult(
            scope.ScopeLabel,
            scope.IsKeeper,
            canRecordStockIn,
            items));
    }
}
