using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IInventoryRebuildRepository
{
    /// <summary>
    /// Tracked balance rows for the scope, locked on PostgreSQL inside the caller's transaction
    /// (the whole scope is locked at once because the rebuild may touch every pair).
    /// </summary>
    Task<IReadOnlyList<StockBalance>> LockBalancesForRebuildAsync(
        int? warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Net sound/damaged quantities derived from every ledger row of the scope.</summary>
    Task<IReadOnlyList<LedgerBalanceRow>> GetLedgerBalancesAsync(
        int? warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Remaining quantity of active warehouse allocations per pair (Q9).</summary>
    Task<IReadOnlyList<ReservedBalanceRow>> GetReservedBalancesAsync(
        int? warehouseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> GetProductNamesAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);

    void AddBalance(StockBalance balance);

    void AddLog(InventoryRebuildLog log);

    Task<IReadOnlyList<RebuildRunRow>> GetHistoryAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task<RebuildLogDetails> GetDetailsAsync(
        int logId,
        CancellationToken cancellationToken = default);
}
