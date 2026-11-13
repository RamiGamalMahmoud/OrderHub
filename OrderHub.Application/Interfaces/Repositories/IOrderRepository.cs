using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    void Add(Order order);
    Task<Order> GetById(int orderId);
    Task<Order> GetForUpdateAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order> GetForLifecycleAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Read-only order with client, items and allocations for issue-order creation.</summary>
    Task<Order> GetForIssueOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Recent open orders that still carry active warehouse allocations.</summary>
    Task<IReadOnlyList<Order>> GetOrdersWithOpenWarehouseAllocationsAsync(
        int take,
        CancellationToken cancellationToken = default);
    Task PrepareDeliveryStepReorderingAsync(Order order, CancellationToken cancellationToken = default);
    Task TombstoneDeliveryStepAsync(OrderDeliveryStep step, CancellationToken cancellationToken = default);
    void RemoveAllocations(IEnumerable<OrderInventoryAllocation> allocations);
    Task<DeliveryParty> FindDeliveryPartyAsync(DeliveryMethod method, int partyId, CancellationToken cancellationToken = default);
    Task<int?> FindCreationRequestAsync(Guid requestId, int userId, CancellationToken cancellationToken = default);
}
