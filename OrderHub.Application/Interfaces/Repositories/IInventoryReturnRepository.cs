using OrderHub.Application.Features.Inventory.Returns.Contracts;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IInventoryReturnRepository
{
    void Add(InventoryReturn record);

    /// <summary>Tracked allocation with its order item, warehouse and order for a return transaction.</summary>
    Task<IReadOnlyList<OrderInventoryAllocation>> GetAllocationsForUpdateAsync(
        IReadOnlyCollection<int> allocationIds,
        CancellationToken cancellationToken = default);

    /// <summary>Σ (intact + damaged) physically returned against one allocation.</summary>
    Task<int> GetReturnedQuantityAsync(int allocationId, CancellationToken cancellationToken = default);

    Task<OrderReturnContext> GetOrderReturnContextAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    /// <summary>Allocations of one warehouse that still carry a «مرتجع مستحق» quantity.</summary>
    Task<IReadOnlyList<PendingReturnRow>> GetPendingAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Orders with issued allocations in the warehouse that can still receive a return.</summary>
    Task<IReadOnlyList<ReturnableOrderRow>> GetReturnableOrdersAsync(
        int warehouseId,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReturnHistoryRow>> GetHistoryAsync(
        int warehouseId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Products with a positive damaged quantity in the warehouse.</summary>
    Task<IReadOnlyList<DamagedProductRow>> GetDamagedProductsAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);
}
