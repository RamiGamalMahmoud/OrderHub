using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IStockIssueOrderRepository
{
    void Add(StockIssueOrder order);

    /// <summary>Tracked aggregate with lines, batches and allocations for batch/cancel mutations.</summary>
    Task<StockIssueOrder> GetForUpdateAsync(
        int issueOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>Read-only aggregate with display names for the details screen.</summary>
    Task<StockIssueOrder> GetDetailsAsync(
        int issueOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>All issue orders of one order (any status), newest first, for the order workflow page.</summary>
    Task<IReadOnlyList<StockIssueOrder>> GetForOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    /// <summary>Non-terminal issue orders of one order, tracked, for the lifecycle auto-cancel hook.</summary>
    Task<IReadOnlyList<StockIssueOrder>> GetActiveForOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockIssueOrder>> GetScopedAsync(
        int? warehouseId,
        int? requestedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Q4: Σ requested quantities of lines in non-terminal orders per allocation.</summary>
    Task<IReadOnlyDictionary<int, int>> GetActiveRequestedByAllocationAsync(
        IReadOnlyCollection<int> allocationIds,
        CancellationToken cancellationToken = default);

    /// <summary>Allocations that already carry an active cancellation request (blocking new orders).</summary>
    Task<IReadOnlyCollection<int>> GetCancelRequestedAllocationIdsAsync(
        IReadOnlyCollection<int> allocationIds,
        CancellationToken cancellationToken = default);
}
