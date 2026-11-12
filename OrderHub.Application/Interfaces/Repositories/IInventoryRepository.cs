using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IInventoryRepository
{
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

    /// <summary>True when the warehouse already has any ledger movement (used by the opening-balance rule).</summary>
    Task<bool> HasMovementsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);
}
