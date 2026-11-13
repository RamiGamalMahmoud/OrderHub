using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.ConsolidatedBalances;

internal sealed class GetProductWarehouseDistributionQueryHandler(
    IWarehouseRepository warehouses,
    IInventoryRepository inventory,
    IAuthorizationService authorization,
    ICurrentUser currentUser)
    : IRequestHandler<GetProductWarehouseDistributionQuery, Result<ProductWarehouseDistributionResult>>
{
    public async Task<Result<ProductWarehouseDistributionResult>> Handle(
        GetProductWarehouseDistributionQuery request,
        CancellationToken cancellationToken)
    {
        Result<InventoryScope> scopeResult = await InventoryScope.ResolveAsync(
            currentUser,
            authorization,
            warehouses,
            cancellationToken);
        if (!scopeResult.IsSuccess)
            return Result<ProductWarehouseDistributionResult>.Failure(
                scopeResult.ErrorMessage,
                scopeResult.ErrorCode);

        InventoryScope scope = scopeResult.Value;
        IReadOnlyCollection<int> scopeIds = scope.IsUnrestricted
            ? null
            : [scope.KeeperWarehouseId];

        IReadOnlyList<ProductDistributionProjectionRow> rows = await inventory
            .GetProductDistributionAsync(request.ProductId, scopeIds, cancellationToken);
        if (rows.Count == 0)
        {
            return Result<ProductWarehouseDistributionResult>.Failure(
                "لا يوجد رصيد لهذا المنتج في المخازن المتاحة لك.",
                "NotFound");
        }

        ProductDistributionProjectionRow first = rows[0];
        int stockOnHand = rows.Sum(row => row.StockOnHand);
        int reservedOnHand = rows.Sum(row => row.ReservedOnHand);
        int damagedQty = rows.Sum(row => row.DamagedQty);

        return Result<ProductWarehouseDistributionResult>.Success(new ProductWarehouseDistributionResult(
            new ProductDistributionProduct(
                first.ProductId,
                first.ProductName,
                first.ProductCode,
                first.CategoryName,
                stockOnHand,
                reservedOnHand,
                damagedQty),
            new WarehouseStockTotals(
                stockOnHand,
                reservedOnHand,
                damagedQty,
                1,
                stockOnHand > 0 ? 1 : 0),
            rows.Select(row => new ProductDistributionWarehouse(
                row.WarehouseId,
                row.WarehouseName,
                row.IsActive,
                row.StockOnHand,
                row.ReservedOnHand,
                row.DamagedQty)).ToList(),
            scope.IsKeeper));
    }
}
