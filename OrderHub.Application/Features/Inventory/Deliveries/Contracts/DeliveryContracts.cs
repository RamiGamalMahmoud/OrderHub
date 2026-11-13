using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Inventory.Deliveries.Contracts;

/// <summary>تسليم مخزني لبند طلب من تخصيص مخزني واحد (D19).</summary>
public sealed record OrderDeliveryRow(
    int AllocationId,
    int OrderItemId,
    int ProductId,
    string ProductName,
    int WarehouseId,
    string WarehouseName,
    int IssuedQty,
    int DeliveredQty)
{
    public int RemainingQty => Math.Max(0, IssuedQty - DeliveredQty);

    public bool CanDeliver => RemainingQty > 0;
}

/// <summary>مشوار أو شحنة مرتبطة بالطلب، لربط سجل التسليم اختياريًا (D16).</summary>
public sealed record DeliveryLinkOption(int Id, long Number, int SequenceYear, int SequenceMonth);

public sealed record OrderDeliveryOverview(
    IReadOnlyList<OrderDeliveryRow> Rows,
    IReadOnlyList<DeliveryLinkOption> Trips,
    IReadOnlyList<DeliveryLinkOption> Shipments);
