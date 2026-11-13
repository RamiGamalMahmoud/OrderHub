using OrderHub.Application.Features.Inventory.Deliveries.Contracts;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IStockDeliveryRepository
{
    void Add(StockDeliveryRecord record);

    /// <summary>Σ delivered quantities of one allocation, used for the `Issued − Delivered` cap (D19).</summary>
    Task<int> GetDeliveredQuantityAsync(
        int allocationId,
        CancellationToken cancellationToken = default);

    /// <summary>Per-allocation delivered/remaining rows plus the order's optional trip/shipment links.</summary>
    Task<OrderDeliveryOverview> GetOrderDeliveryOverviewAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    /// <summary>True when the optional trip/shipment link (if any) actually belongs to the order.</summary>
    Task<bool> IsLinkValidAsync(
        int orderId,
        int? tripId,
        int? shipmentId,
        CancellationToken cancellationToken = default);
}
