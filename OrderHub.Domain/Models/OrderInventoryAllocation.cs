using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

/// <summary>
/// تخصيص كمية بند الطلب على مصدر: مخزن محدد أو المورد المرتبط.
/// </summary>
public class OrderInventoryAllocation : ModelBase
{
    private OrderInventoryAllocation()
    {
    }

    private OrderInventoryAllocation(
        int productId,
        AllocationSourceKind sourceKind,
        int qty,
        int? warehouseId,
        int? supplierId)
    {
        ProductId = productId;
        SourceKind = sourceKind;
        Qty = qty;
        WarehouseId = sourceKind == AllocationSourceKind.Warehouse ? warehouseId : null;
        SupplierId = sourceKind == AllocationSourceKind.Supplier ? supplierId : null;
        Status = OrderAllocationStatus.Active;
    }

    public int OrderId { get; private set; }
    public Order Order { get; private set; }

    public int OrderItemId { get; private set; }
    public OrderItem OrderItem { get; private set; }

    public int ProductId { get; private set; }

    public AllocationSourceKind SourceKind { get; private set; }

    public int? WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; }

    public int? SupplierId { get; private set; }
    public Supplier Supplier { get; private set; }

    public int Qty { get; private set; }

    public int IssuedQty { get; private set; }

    /// <summary>كمية مصروفة تجاوزت الطلب بعد تعديل معتمد؛ تنتظر «مرتجع مستحق» (T08).</summary>
    public int PendingReturnQty { get; private set; }

    public OrderAllocationStatus Status { get; private set; }

    public int Remaining => Qty - IssuedQty;

    public static Result<OrderInventoryAllocation> Create(
        int productId,
        AllocationSourceKind sourceKind,
        int qty,
        int? warehouseId = null,
        int? supplierId = null)
    {
        if (productId <= 0)
            return Result<OrderInventoryAllocation>.Failure("المنتج غير صالح.");

        if (qty <= 0)
            return Result<OrderInventoryAllocation>.Failure("كمية التخصيص يجب أن تكون أكبر من صفر.");

        if (sourceKind == AllocationSourceKind.Warehouse)
        {
            if (warehouseId is not > 0)
                return Result<OrderInventoryAllocation>.Failure("يجب تحديد المخزن لتخصيص المخزون.");
        }
        else if (sourceKind == AllocationSourceKind.Supplier)
        {
            if (supplierId is not > 0)
                return Result<OrderInventoryAllocation>.Failure("يجب تحديد المورد لتخصيص التوريد.");
        }
        else
        {
            return Result<OrderInventoryAllocation>.Failure("مصدر التخصيص غير صالح.");
        }

        return Result<OrderInventoryAllocation>.Success(
            new OrderInventoryAllocation(productId, sourceKind, qty, warehouseId, supplierId));
    }

    internal void AttachTo(Order order, OrderItem item)
    {
        if (Id > 0 && Order is not null && !ReferenceEquals(Order, order))
            throw new System.InvalidOperationException("لا يمكن نقل تخصيص محفوظ إلى طلب آخر.");

        Order = order;
        OrderId = order.Id;
        OrderItem = item;
        OrderItemId = item.Id;
        ProductId = item.ProductId;
    }

    public Result ChangeQuantity(int qty)
    {
        if (Status != OrderAllocationStatus.Active)
            return Result.Failure("لا يمكن تعديل تخصيص غير نشط.");

        if (qty <= 0)
            return Result.Failure("كمية التخصيص يجب أن تكون أكبر من صفر.");

        if (qty < IssuedQty)
            return Result.Failure("لا يمكن تقليل التخصيص عن الكمية المصروفة فعليًا.");

        if (Qty == qty)
            return Result.Success();

        Qty = qty;
        UpdateModificationDate(System.DateTime.UtcNow);
        return Result.Success();
    }

    /// <summary>Registers an issued batch against this allocation (Q5).</summary>
    public Result Consume(int quantity)
    {
        if (Status != OrderAllocationStatus.Active)
            return Result.Failure("لا يمكن صرف كمية من تخصيص غير نشط.");

        if (quantity <= 0)
            return Result.Failure("كمية الصرف يجب أن تكون أكبر من صفر.");

        if (IssuedQty + quantity > Qty)
            return Result.Failure("كمية الصرف تتجاوز الكمية المخصصة لهذا المصدر.");

        IssuedQty += quantity;
        UpdateModificationDate(System.DateTime.UtcNow);
        return Result.Success();
    }

    /// <summary>Rolls back an issued quantity after an approved batch reversal.</summary>
    public Result RollBackIssue(int quantity)
    {
        if (Status != OrderAllocationStatus.Active)
            return Result.Failure("لا يمكن عكس صرف لتخصيص غير نشط.");

        if (quantity <= 0)
            return Result.Failure("كمية العكس يجب أن تكون أكبر من صفر.");

        if (quantity > IssuedQty)
            return Result.Failure("كمية العكس تتجاوز الكمية المصروفة لهذا المصدر.");

        IssuedQty -= quantity;
        UpdateModificationDate(System.DateTime.UtcNow);
        return Result.Success();
    }

    /// <summary>Sets the issued quantity that exceeds the approved demand as a pending return.</summary>
    public Result SetPendingReturn(int quantity)
    {
        if (quantity < 0 || quantity > IssuedQty)
            return Result.Failure("كمية المرتجع المستحق غير صالحة.");

        PendingReturnQty = quantity;
        UpdateModificationDate(System.DateTime.UtcNow);
        return Result.Success();
    }

    /// <summary>Clears a physically received part of the pending return; settles each unit once.</summary>
    public Result SettlePendingReturn(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("كمية الاستلام يجب أن تكون أكبر من صفر.");

        if (quantity > PendingReturnQty)
            return Result.Failure("كمية الاستلام تتجاوز المرتجع المستحق على هذا التخصيص.");

        PendingReturnQty -= quantity;
        UpdateModificationDate(System.DateTime.UtcNow);
        return Result.Success();
    }

    public Result Release()
    {
        if (Status != OrderAllocationStatus.Active)
            return Result.Failure("لا يمكن تحرير تخصيص غير نشط.");

        Status = OrderAllocationStatus.Released;
        UpdateModificationDate(System.DateTime.UtcNow);
        return Result.Success();
    }
}
