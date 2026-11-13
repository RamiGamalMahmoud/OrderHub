using OrderHub.Application.Features.Inventory.Approvals.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IInventoryApprovalRepository
{
    void Add(InventoryApprovalRequest request);

    /// <summary>Tracked request loaded for the single decision (version concurrency token enforces the CAS).</summary>
    Task<InventoryApprovalRequest> GetForUpdateAsync(int approvalId, CancellationToken cancellationToken = default);

    Task<InventoryApprovalProjectionRow> GetRowAsync(int approvalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryApprovalProjectionRow>> GetRowsAsync(
        InventoryApprovalStatus? status,
        InventoryApprovalRequestType? requestType,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingEditAsync(int orderId, CancellationToken cancellationToken = default);

    Task<bool> HasPendingReversalAsync(int movementId, CancellationToken cancellationToken = default);
}
