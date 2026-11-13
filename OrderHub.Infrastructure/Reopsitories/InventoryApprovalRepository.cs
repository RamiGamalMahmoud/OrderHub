using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Inventory.Approvals.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal sealed class InventoryApprovalRepository : IInventoryApprovalRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryApprovalRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(InventoryApprovalRequest request) => _dbContext.InventoryApprovalRequests.Add(request);

    public Task<InventoryApprovalRequest> GetForUpdateAsync(
        int approvalId,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryApprovalRequests
            .SingleOrDefaultAsync(request => request.Id == approvalId, cancellationToken);

    public Task<InventoryApprovalProjectionRow> GetRowAsync(
        int approvalId,
        CancellationToken cancellationToken = default) =>
        ProjectRows(_dbContext.InventoryApprovalRequests
            .AsNoTracking()
            .Where(request => request.Id == approvalId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryApprovalProjectionRow>> GetRowsAsync(
        InventoryApprovalStatus? status,
        InventoryApprovalRequestType? requestType,
        CancellationToken cancellationToken = default)
    {
        IQueryable<InventoryApprovalRequest> query = _dbContext.InventoryApprovalRequests.AsNoTracking();
        if (status.HasValue)
            query = query.Where(request => request.Status == status.Value);
        if (requestType.HasValue)
            query = query.Where(request => request.RequestType == requestType.Value);

        return await ProjectRows(query
                .OrderByDescending(request => request.Status == InventoryApprovalStatus.Pending)
                .ThenByDescending(request => request.CreatedAt)
                .ThenByDescending(request => request.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasPendingEditAsync(int orderId, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryApprovalRequests.AsNoTracking().AnyAsync(
            request => request.OrderId == orderId &&
                request.RequestType == InventoryApprovalRequestType.AllocationEdit &&
                request.Status == InventoryApprovalStatus.Pending,
            cancellationToken);

    public Task<bool> HasPendingReversalAsync(int movementId, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryApprovalRequests.AsNoTracking().AnyAsync(
            request => request.InventoryMovementId == movementId &&
                request.RequestType == InventoryApprovalRequestType.MovementReversal &&
                request.Status == InventoryApprovalStatus.Pending,
            cancellationToken);

    public Task<bool> HasPendingDisposalAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryApprovalRequests.AsNoTracking().AnyAsync(
            request => request.WarehouseId == warehouseId &&
                request.ProductId == productId &&
                request.RequestType == InventoryApprovalRequestType.DamagedDisposal &&
                request.Status == InventoryApprovalStatus.Pending,
            cancellationToken);

    private IQueryable<InventoryApprovalProjectionRow> ProjectRows(
        IQueryable<InventoryApprovalRequest> requests) =>
        from request in requests
        join requestedBy in _dbContext.Users.AsNoTracking()
            on request.RequestedByUserId equals requestedBy.Id
        join decidedBy in _dbContext.Users.AsNoTracking()
            on request.DecidedByUserId equals decidedBy.Id into decidedGroup
        from decidedBy in decidedGroup.DefaultIfEmpty()
        join order in _dbContext.Orders.AsNoTracking()
            on request.OrderId equals order.Id into orderGroup
        from order in orderGroup.DefaultIfEmpty()
        join issueOrder in _dbContext.StockIssueOrders.AsNoTracking()
            on request.StockIssueOrderId equals issueOrder.Id into issueGroup
        from issueOrder in issueGroup.DefaultIfEmpty()
        join issueWarehouse in _dbContext.Warehouses.AsNoTracking()
            on issueOrder.WarehouseId equals issueWarehouse.Id into issueWarehouseGroup
        from issueWarehouse in issueWarehouseGroup.DefaultIfEmpty()
        join issueOrderTarget in _dbContext.Orders.AsNoTracking()
            on issueOrder.OrderId equals issueOrderTarget.Id into issueOrderTargetGroup
        from issueOrderTarget in issueOrderTargetGroup.DefaultIfEmpty()
        join movement in _dbContext.InventoryMovements.AsNoTracking()
            on request.InventoryMovementId equals movement.Id into movementGroup
        from movement in movementGroup.DefaultIfEmpty()
        join movementWarehouse in _dbContext.Warehouses.AsNoTracking()
            on movement.WarehouseId equals movementWarehouse.Id into movementWarehouseGroup
        from movementWarehouse in movementWarehouseGroup.DefaultIfEmpty()
        join movementProduct in _dbContext.Products.AsNoTracking()
            on movement.ProductId equals movementProduct.Id into movementProductGroup
        from movementProduct in movementProductGroup.DefaultIfEmpty()
        join disposalWarehouse in _dbContext.Warehouses.AsNoTracking()
            on request.WarehouseId equals disposalWarehouse.Id into disposalWarehouseGroup
        from disposalWarehouse in disposalWarehouseGroup.DefaultIfEmpty()
        join disposalProduct in _dbContext.Products.AsNoTracking()
            on request.ProductId equals disposalProduct.Id into disposalProductGroup
        from disposalProduct in disposalProductGroup.DefaultIfEmpty()
        select new InventoryApprovalProjectionRow(
            request.Id,
            request.RequestType,
            request.Status,
            request.Reason,
            requestedBy.DisplayName ?? requestedBy.UserName,
            request.RequestedByUserId,
            request.CreatedAt,
            request.OrderId,
            order == null ? null : order.OrderNumber,
            request.StockIssueOrderId,
            issueOrder == null ? null : issueOrder.WorkRecordNumber,
            issueOrder == null ? null : issueOrder.SequenceYear,
            issueOrder == null ? null : issueOrder.SequenceMonth,
            issueWarehouse == null ? null : issueWarehouse.Name.Value,
            issueOrderTarget == null ? null : issueOrderTarget.OrderNumber,
            request.InventoryMovementId,
            movementWarehouse == null ? null : movementWarehouse.Name.Value,
            movementProduct == null ? null : movementProduct.Name.Value,
            movement == null ? 0 : movement.Quantity,
            movement == null ? (InventoryDirection?)null : movement.Direction,
            movement == null ? (InventoryMovementType?)null : movement.MovementType,
            movement == null ? (InventoryMovementStatus?)null : movement.Status,
            movement == null ? null : movement.EffectiveAtUtc,
            disposalWarehouse == null ? null : disposalWarehouse.Name.Value,
            disposalProduct == null ? null : disposalProduct.Name.Value,
            request.DisposalQuantity,
            request.DecidedByUserId,
            decidedBy == null ? null : decidedBy.DisplayName ?? decidedBy.UserName,
            request.DecidedAtUtc,
            request.DecisionNote,
            request.Version);
}
