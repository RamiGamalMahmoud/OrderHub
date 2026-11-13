using OrderHub.Application.Features.Inventory.Counts.Contracts;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IInventoryCountRepository
{
    void Add(InventoryCount count);

    /// <summary>Tracked count with its active lines and warehouse; locks the header row on PostgreSQL.</summary>
    Task<InventoryCount> GetForUpdateAsync(int countId, CancellationToken cancellationToken = default);

    /// <summary>True when any open count line still covers the (warehouse, product) pair.</summary>
    Task<bool> HasActiveLineAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default);

    /// <summary>Sound and damaged book quantities from the ledger up to the given effective moment.</summary>
    Task<BookQuantities> GetBookQuantitiesAsync(
        int warehouseId,
        int productId,
        DateTime upToUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CountableProductRow>> GetCountableProductsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CountRow>> GetRowsAsync(
        int? warehouseId,
        int take,
        CancellationToken cancellationToken = default);

    Task<CountDetails> GetDetailsAsync(int countId, CancellationToken cancellationToken = default);

    /// <summary>Active lines that have movements recorded after their snapshot with an older effective time.</summary>
    Task<IReadOnlyList<int>> GetLateMovementLineIdsAsync(
        InventoryCount count,
        CancellationToken cancellationToken = default);
}
