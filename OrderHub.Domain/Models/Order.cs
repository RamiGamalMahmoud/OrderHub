using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.ValueObjects;
using OrderHub.Domain.Services.Pricing;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public partial class Order : ModelBase
{
    private readonly List<OrderItem> _orderItems = [];
    private readonly List<OrderInventoryAllocation> _allocations = [];
    private readonly List<OrderDeliveryStep> _deliverySteps = [];
    private readonly List<Shipment> _shipments = [];
    private readonly List<DeliveryTrip> _deliveryTrips = [];
    private readonly List<Supply> _supplies = [];
    
    private Order()
    {
    }

    public Order(int clientId, string orderNumber, System.Guid? creationRequestId = null)
    {
        ClientId = clientId;
        OrderNumber = orderNumber;
        OrderStatus = OrderStatus.Processing;
        CreationRequestId = creationRequestId;
    }

    public int ClientId { get; private set; }
    public Client Client { get; private set; }

    public string OrderNumber { get; private set; }
    public System.Guid? CreationRequestId { get; private set; }
    public OrderSource? OrderSource { get; private set; }

    public DeliveryMethod? DeliveryMethod { get; private set; }

    public OrderStatus OrderStatus { get; private set; }

    public decimal DeliveryCost { get; private set; }
    public DiscountType DiscountType { get; private set; } = DiscountType.FixedAmount;
    public decimal DiscountValue { get; private set; }
    public decimal DiscountAmount { get; private set; }

    public int? ShippingCarrierId { get; private set; }
    public ShippingCarrier ShippingCarrier { get; private set; }

    public int? DeliverymanId { get; private set; }
    public Deliveryman Deliveryman { get; private set; }

    public int? PaymentMethodId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }

    public PricingResult Pricing => _orderItems.Count == 0
        ? new PricingResult([], 0m, 0m, 0m)
        : PricingCalculator.Calculate(_orderItems.Select(item => new PricingItem(
            item.ProductId, item.ProductName, item.Quantity, item.UnitPrice.Value, item.VatRate)).ToList());

    public Money Total => new(OrderFinancialCalculator.CalculateFinalTotal(Pricing.TotalAmount, DiscountAmount, DeliveryCost));

    public Result SetFinancialAdjustments(decimal deliveryCost, DiscountType discountType, decimal discountValue)
    {
        if (!CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        var calculated = OrderFinancialCalculator.Calculate(Pricing, deliveryCost, discountType, discountValue);
        if (!calculated.IsSuccess) return Result.Failure(calculated.ErrorMessage);
        DeliveryCost = deliveryCost;
        DiscountType = discountType;
        DiscountValue = discountValue;
        DiscountAmount = calculated.Value.DiscountAmount;
        return Result.Success();
    }

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public IReadOnlyCollection<OrderInventoryAllocation> Allocations => _allocations.AsReadOnly();
    public IReadOnlyCollection<OrderDeliveryStep> DeliverySteps => _deliverySteps.AsReadOnly();
    public IReadOnlyCollection<Shipment> Shipments => _shipments.AsReadOnly();
    public IReadOnlyCollection<DeliveryTrip> DeliveryTrips => _deliveryTrips.AsReadOnly();
    public IReadOnlyCollection<Supply> Supplies => _supplies.AsReadOnly();

    private void StartDirectWorkRecord()
    {
        if (DeliveryMethod == Enums.DeliveryMethod.ShippingCompany) _shipments.Add(new Shipment(this, null));
        else if (DeliveryMethod == Enums.DeliveryMethod.DeliveryMan) _deliveryTrips.Add(new DeliveryTrip(this, null));
    }

    public void UpdateHeader(
        int clientId,
        OrderSource orderSource,
        DeliveryMethod deliveryMethod,
        int? deliverymanId,
        int? shippingCarrierId,
        int? paymentMethodId)
    {
        EnsureEditable();
        ClientId = clientId;
        OrderSource = orderSource;
        DeliveryMethod = deliveryMethod;
        DeliverymanId = deliverymanId;
        ShippingCarrierId = shippingCarrierId;
        PaymentMethodId = paymentMethodId;
    }

    public Result<OrderItem> AddOrderItem(
        int productId,
        string productName,
        decimal unitPrice,
        int quantity,
        decimal vatRate,
        string supplierName,
        int? supplierId,
        IReadOnlyCollection<OrderItemPropertyData> properties)
    {
        if (!CanEdit) return Result<OrderItem>.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        Result<OrderItem> result = OrderItem.Create(
            productId,
            productName,
            unitPrice,
            quantity,
            vatRate,
            supplierName,
            supplierId,
            properties);

        if (result.IsSuccess)
        {
            result.Value.AttachTo(this);
            _orderItems.Add(result.Value);
            EnsureSupplierSupplies();
            RefreshDiscount();
        }

        return result;
    }

    public void AddAllocation(OrderInventoryAllocation allocation, OrderItem item)
    {
        EnsureEditable();
        allocation.AttachTo(this, item);
        _allocations.Add(allocation);
    }

    public void RemoveAllocations(IReadOnlyCollection<OrderInventoryAllocation> allocations)
    {
        EnsureEditable();
        _allocations.RemoveAll(allocation => allocations.Any(dropped => ReferenceEquals(dropped, allocation)));
    }

    public void RemoveOrderItem(OrderItem orderItem)
    {
        EnsureEditable();
        _orderItems.RemoveAll(item => ReferenceEquals(item, orderItem));
        EnsureSupplierSupplies();
        RefreshDiscount();
    }

    public void ClearOrderItems()
    {
        EnsureEditable();
        _orderItems.Clear();
        EnsureSupplierSupplies();
        RefreshDiscount();
    }

    public void AddDeliveryStep(OrderDeliveryStep deliveryStep)
    {
        EnsureEditable();
        deliveryStep.AttachTo(this);
        _deliverySteps.Add(deliveryStep);
    }

    public Result ChangeClient(int clientId)
    {
        if (!CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        ClientId = clientId;

        return Result.Success();
    }

    public Result ChangeDeliveryman(int? deliverymanId)
    {
        if (!CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        DeliverymanId = deliverymanId;
        return Result.Success();
    }

    public Result ChangeShippingCarrier(int? shippingCarrierId)
    {
        if (!CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        ShippingCarrierId = shippingCarrierId;
        return Result.Success();
    }

    public Result ChangePaymentMethod(int? paymentMethodId)
    {
        if (!CanEdit) return Result.Failure("بيانات الطلب مقفولة ولا تقبل التعديل.");
        if (PaymentMethodId == paymentMethodId)
            return Result.Success();

        PaymentMethodId = paymentMethodId;

        return Result.Success();
    }

    public void ClearDeliverySteps()
    {
        EnsureEditable();
        _deliverySteps.Clear();
    }

    public void ReplaceOrderItems(IReadOnlyCollection<OrderItem> items)
    {
        EnsureEditable();
        // Materialize before clearing, including when the caller passes our own items.
        var replacementItems = items.ToList();
        foreach (var item in replacementItems) item.AttachTo(this);
        _orderItems.Clear();
        _orderItems.AddRange(replacementItems);
        EnsureSupplierSupplies();
        RefreshDiscount();
    }

    /// <summary>
    /// Applies an editor payload while preserving the identity of persisted items.
    /// </summary>
    public void SynchronizeOrderItems(IReadOnlyCollection<(int? Id, OrderItem Values)> items)
    {
        EnsureEditable();
        var existing = _orderItems.ToDictionary(item => item.Id);
        var ids = items.Where(item => item.Id.HasValue).Select(item => item.Id.Value).ToList();
        if (ids.Distinct().Count() != ids.Count || ids.Any(id => id <= 0 || !existing.ContainsKey(id)))
            throw new System.ArgumentException("Invalid order item identities.", nameof(items));

        var synchronized = new List<OrderItem>();
        foreach (var (id, values) in items)
        {
            if (id.HasValue)
            {
                var item = existing[id.Value];
                var changed = item.Synchronize(
                    values.UnitPrice.Value,
                    values.Quantity,
                    values.VatRate,
                    values.SupplierName,
                    values.SupplierId,
                    values.Properties.Select(property => new OrderItemPropertyData(
                        property.PropertyId, property.Value)).ToList());
                if (!changed.IsSuccess)
                    throw new System.InvalidOperationException(changed.ErrorMessage);
                synchronized.Add(item);
            }
            else
            {
                values.AttachTo(this);
                synchronized.Add(values);
            }
        }

        var removed = _orderItems.Where(item => synchronized.All(kept => !ReferenceEquals(kept, item))).ToList();
        foreach (var item in removed)
        {
            if (_allocations.Any(allocation => allocation.OrderItemId == item.Id &&
                    allocation.Status == OrderAllocationStatus.Active))
                throw new System.InvalidOperationException(
                    "لا يمكن حذف بند مرتبط بتخصيص مخزون فعّال. حرر التخصيص أولاً.");
        }

        foreach (var item in removed)
        {
            _allocations.RemoveAll(allocation => allocation.OrderItemId == item.Id);
            _orderItems.Remove(item);
        }

        foreach (var item in synchronized)
            if (!_orderItems.Any(existingItem => ReferenceEquals(existingItem, item)))
                _orderItems.Add(item);

        EnsureSupplierSupplies();
        RefreshDiscount();
    }

    public void SynchronizeDeliverySteps(IReadOnlyCollection<(int? Id, OrderDeliveryStep Values)> steps)
    {
        EnsureEditable();
        // Validate all identities before changing the aggregate.
        var existing = _deliverySteps.ToDictionary(step => step.Id);
        var ids = steps.Where(step => step.Id.HasValue).Select(step => step.Id.Value).ToList();
        if (ids.Distinct().Count() != ids.Count || ids.Any(id => id <= 0 || !existing.ContainsKey(id)))
            throw new System.ArgumentException("Invalid delivery step identities.", nameof(steps));

        var synchronized = new List<OrderDeliveryStep>();
        foreach (var (id, values) in steps)
        {
            var step = id.HasValue ? existing[id.Value] : values;
            step.StepOrder = values.StepOrder;
            step.DeliveryMethod = values.DeliveryMethod;
            step.DeliverymanId = values.DeliverymanId;
            step.ShippingCarrierId = values.ShippingCarrierId;
            step.AttachTo(this);
            synchronized.Add(step);
        }
        // Keep the matched instances. New unsaved entities share Id=0, so identity
        // must not be compared using ModelBase.Equals when retaining the final graph.
        _deliverySteps.Clear();
        _deliverySteps.AddRange(synchronized);
    }

    public void ChangeDeliveryMethod(DeliveryMethod deliveryMethod)
    {
        EnsureEditable();
        DeliveryMethod = deliveryMethod;
    }
    public void ChangeOrderSource(OrderSource orderSource)
    {
        EnsureEditable();
        OrderSource = orderSource;
    }

    private void EnsureEditable()
    {
        if (!CanEdit) throw new System.InvalidOperationException("بيانات الطلب مقفولة ولا تقبل التعديل.");
    }

    private void EnsureSupplierSupplies()
    {
        foreach (var supplierId in _orderItems.Where(item => item.SupplierId.HasValue)
                     .Select(item => item.SupplierId.Value).Distinct())
            if (!_supplies.Any(supply => supply.SupplierId == supplierId))
                _supplies.Add(new Supply(this, supplierId));
    }

    internal void RefreshDiscount()
    {
        var subtotal = _orderItems.Sum(item => item.SubTotal.Value);
        DiscountAmount = _orderItems.Count == 0 ? 0m : DiscountType == DiscountType.Percentage
            ? subtotal * DiscountValue / 100m : DiscountValue;
    }
}
