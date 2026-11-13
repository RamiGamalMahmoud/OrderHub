using OrderHub.Application.Features.Inventory.ConsolidatedBalances;
using OrderHub.Application.Features.Inventory.GetStockAvailability;
using OrderHub.Application.Features.Inventory.WarehouseLedger;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IInventoryRepository
{
    /// <summary>Company-wide availability of a product per active warehouse, highest available first.</summary>
    Task<IReadOnlyList<StockAvailabilityRow>> GetAvailabilityAsync(
        int productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ordered row locks (ascending product, then warehouse) on PostgreSQL; a no-op elsewhere.
    /// Must run inside the caller's transaction (D13).
    /// </summary>
    Task LockBalancesAsync(
        IReadOnlyCollection<(int WarehouseId, int ProductId)> keys,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the tracked balance row so domain behaviors can be applied before saving.</summary>
    Task<StockBalance> GetStockItemAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockBalance>> GetStockAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the tracked balance row for the pair, creating it when it does not exist yet.</summary>
    Task<StockBalance> GetOrCreateStockItemAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default);

    void Add(InventoryMovement movement);

    /// <summary>Tracked ledger row for an approved reversal.</summary>
    Task<InventoryMovement> GetMovementForUpdateAsync(
        int movementId,
        CancellationToken cancellationToken = default);

    /// <summary>Atomic guarded reservation; returns false when the available quantity is insufficient.</summary>
    Task<bool> TryReserveAsync(
        int warehouseId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>Atomic guarded release of a previously reserved quantity.</summary>
    Task<bool> ReleaseAsync(
        int warehouseId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>Atomic guarded issue: removes the quantity from both stock and reservation.</summary>
    Task<bool> ConsumeAsync(
        int warehouseId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovement>> GetLedgerAsync(
        int warehouseId,
        int? productId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountLedgerAsync(
        int warehouseId,
        int? productId,
        CancellationToken cancellationToken = default);

    /// <summary>Ledger page with performer name and the receipt/issue-order references, newest first.</summary>
    Task<IReadOnlyList<WarehouseLedgerProjectionRow>> GetWarehouseLedgerPageAsync(
        int warehouseId,
        int productId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Current balance of one product inside a warehouse, or null when no balance row exists.</summary>
    Task<WarehouseProductStockRow> GetWarehouseProductStockAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default);

    /// <summary>Full totals of a warehouse (ignores every filter).</summary>
    Task<WarehouseStockTotalsRow> GetWarehouseStockTotalsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Totals after the category/status filters only (search and hide-zero do not affect cards).</summary>
    Task<WarehouseStockTotalsRow> GetWarehouseStockFilteredTotalsAsync(
        WarehouseStockQueryFilter filter,
        CancellationToken cancellationToken = default);

    Task<int> CountWarehouseStockAsync(
        WarehouseStockQueryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Count of the filtered rows that carry positive physical stock.</summary>
    Task<int> CountWarehouseStockWithStockAsync(
        WarehouseStockQueryFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStockProjectionRow>> GetWarehouseStockPageAsync(
        WarehouseStockQueryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Categories that actually have products in the warehouse, for the filter select.</summary>
    Task<IReadOnlyList<StockCategoryOption>> GetWarehouseStockCategoriesAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>True when the warehouse already has any ledger movement (used by the opening-balance rule).</summary>
    Task<bool> HasMovementsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Full totals of the scoped warehouses after summing every product across them (filters ignored).
    /// A null <paramref name="warehouseIds"/> means every warehouse.
    /// </summary>
    Task<WarehouseStockTotalsRow> GetConsolidatedStockTotalsAsync(
        IReadOnlyCollection<int> warehouseIds,
        CancellationToken cancellationToken = default);

    /// <summary>Totals after the category/status filters only (search and hide-zero do not affect cards).</summary>
    Task<WarehouseStockTotalsRow> GetConsolidatedStockFilteredTotalsAsync(
        ConsolidatedStockQueryFilter filter,
        CancellationToken cancellationToken = default);

    Task<int> CountConsolidatedStockAsync(
        ConsolidatedStockQueryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Count of the filtered products whose aggregated physical stock is positive.</summary>
    Task<int> CountConsolidatedStockWithStockAsync(
        ConsolidatedStockQueryFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsolidatedStockProjectionRow>> GetConsolidatedStockPageAsync(
        ConsolidatedStockQueryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Categories that actually have products in the scoped warehouses, for the filter select.</summary>
    Task<IReadOnlyList<StockCategoryOption>> GetConsolidatedStockCategoriesAsync(
        IReadOnlyCollection<int> warehouseIds,
        CancellationToken cancellationToken = default);

    /// <summary>Balance of one product in every scoped warehouse that holds a row for it, regardless of quantity.</summary>
    Task<IReadOnlyList<ProductDistributionProjectionRow>> GetProductDistributionAsync(
        int productId,
        IReadOnlyCollection<int> warehouseIds,
        CancellationToken cancellationToken = default);
}
