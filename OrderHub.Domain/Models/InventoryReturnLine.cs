using System;
using OrderHub.Domain.Common;

namespace OrderHub.Domain.Models;

/// <summary>
/// سطر مرتجع على تخصيص مخزني واحد؛ يقسم الكمية إلى سليمة وتالفة ويُغلق المرتجع المستحق عند الحاجة.
/// </summary>
public sealed class InventoryReturnLine : ModelBase
{
    private InventoryReturnLine() { }

    public int ReturnId { get; private set; }

    public InventoryReturn Return { get; private set; }

    public int AllocationId { get; private set; }

    public OrderInventoryAllocation Allocation { get; private set; }

    public int OrderItemId { get; private set; }

    public OrderItem OrderItem { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; }

    public int IntactQty { get; private set; }

    public int DamagedQty { get; private set; }

    /// <summary>الكمية التي أغلقت من «مرتجع مستحق» على هذا التخصيص بهذا الاستلام.</summary>
    public int PendingSettledQty { get; private set; }

    public int TotalQty => IntactQty + DamagedQty;

    public static Result<InventoryReturnLine> Create(
        int allocationId,
        int orderItemId,
        int productId,
        int intactQty,
        int damagedQty,
        int pendingSettledQty = 0)
    {
        if (allocationId <= 0 || orderItemId <= 0 || productId <= 0)
            return Result<InventoryReturnLine>.Failure("سطر المرتجع لا يشير إلى تخصيص صالح.");

        if (intactQty < 0 || damagedQty < 0)
            return Result<InventoryReturnLine>.Failure("كميات المرتجع لا تكون سالبة.");

        if (intactQty + damagedQty <= 0)
            return Result<InventoryReturnLine>.Failure("أدخل كمية سليمة أو تالفة أكبر من صفر.");

        if (pendingSettledQty < 0 || pendingSettledQty > intactQty + damagedQty)
            return Result<InventoryReturnLine>.Failure("كمية إغلاق المرتجع المستحق غير صالحة.");

        return Result<InventoryReturnLine>.Success(new InventoryReturnLine
        {
            AllocationId = allocationId,
            OrderItemId = orderItemId,
            ProductId = productId,
            IntactQty = intactQty,
            DamagedQty = damagedQty,
            PendingSettledQty = pendingSettledQty
        });
    }

    internal void AttachTo(InventoryReturn record)
    {
        Return = record;
        ReturnId = record.Id;
    }
}
