using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public partial class Order : ModelBase
{
    private readonly List<OrderItem> _orderItems = [];
    private readonly List<OrderDeliveryStep> _deliverySteps = [];
    
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

    public int? ShippingCarrierId { get; private set; }
    public ShippingCarrier ShippingCarrier { get; private set; }

    public int? DeliverymanId { get; private set; }
    public Deliveryman Deliveryman { get; private set; }

    public int? PaymentMethodId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }

    public Money Total => new(_orderItems.Sum(i => i.SubTotal.Value));

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public IReadOnlyCollection<OrderDeliveryStep> DeliverySteps => _deliverySteps.AsReadOnly();

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
        }

        return result;
    }

    public void RemoveOrderItem(OrderItem orderItem)
    {
        EnsureEditable();
        _orderItems.Remove(orderItem);
    }

    public void ClearOrderItems()
    {
        EnsureEditable();
        _orderItems.Clear();
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
}
