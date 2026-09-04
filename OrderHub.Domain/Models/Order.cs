using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public class Order : ModelBase
{
    private readonly List<OrderItem> _orderItems = [];
    private readonly List<OrderDeliveryStep> _deliverySteps = [];
    private readonly List<OrderEntitySequence> _entitySequences = [];
    private readonly List<OutboxMessage> _outboxMessages = [];

    private Order()
    {
    }

    public Order(int clientId, string orderNumber)
    {
        ClientId = clientId;
        OrderNumber = orderNumber;
    }

    public int ClientId { get; private set; }
    public Client Client { get; private set; }

    public string OrderNumber { get; private set; }

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
    public IReadOnlyCollection<OrderEntitySequence> EntitySequences => _entitySequences.AsReadOnly();
    public IReadOnlyCollection<OutboxMessage> OutboxMessages => _outboxMessages.AsReadOnly();

    public void UpdateHeader(
        int clientId,
        DeliveryMethod deliveryMethod,
        int? deliverymanId,
        int? shippingCarrierId,
        int? paymentMethodId)
    {
        ClientId = clientId;
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
        string supplierName,
        int? supplierId,
        IReadOnlyCollection<OrderItemPropertyData> properties)
    {
        Result<OrderItem> result = OrderItem.Create(
            productId,
            productName,
            unitPrice,
            quantity,
            supplierName,
            supplierId,
            properties);

        if (result.IsSuccess)
        {
            _orderItems.Add(result.Value);
        }

        return result;
    }

    public void RemoveOrderItem(OrderItem orderItem)
    {
        _orderItems.Remove(orderItem);
    }

    public void ClearOrderItems()
    {
        _orderItems.Clear();
    }

    public void AddDeliveryStep(OrderDeliveryStep deliveryStep)
    {
        _deliverySteps.Add(deliveryStep);
    }

    public Result ChangeClient(int clientId)
    {
        ClientId = clientId;

        return Result.Success();
    }

    public Result ChangeDeliveryman(int? deliverymanId)
    {
        DeliverymanId = deliverymanId;
        return Result.Success();
    }

    public Result ChangeShippingCarrier(int? shippingCarrierId)
    {
        ShippingCarrierId = shippingCarrierId;
        return Result.Success();
    }

    public Result ChangePaymentMethod(int? paymentMethodId)
    {
        if (PaymentMethodId == paymentMethodId)
            return Result.Success();

        PaymentMethodId = paymentMethodId;

        return Result.Success();
    }

    public void ClearDeliverySteps()
    {
        _deliverySteps.Clear();
    }

    public void AddEntitySequence(OrderEntitySequence entitySequence)
    {
        _entitySequences.Add(entitySequence);
    }

    public void RemoveEntitySequence(OrderEntitySequence entitySequence)
    {
        _entitySequences.Remove(entitySequence);
    }

    public void ChangeOrderStatus(OrderStatus orderStatus)
    {
        OrderStatus = orderStatus;
    }

    public string GetDisplayTitle(
        RecipientType recipientType,
        int? entityId = null)
    {
        int resolvedEntityId = entityId ?? recipientType switch
        {
            RecipientType.Client => ClientId,
            RecipientType.Deliveryman => DeliverymanId ?? 0,
            _ => 0
        };

        if (resolvedEntityId <= 0)
            return OrderNumber;

        return _entitySequences
            .FirstOrDefault(sequence =>
                sequence.RecipientType == recipientType &&
                sequence.EntityId == resolvedEntityId)
            ?.DisplayTitle
            ?? OrderNumber;
    }

    public void ChangeDeliveryMethod(DeliveryMethod deliveryMethod)
    {
        DeliveryMethod = deliveryMethod;
    }

    public void ReturnToPending() => OrderStatus = OrderStatus.Pending;
    public void Ship() => OrderStatus = OrderStatus.Shipped;
    public void StartProcessing() => OrderStatus = OrderStatus.Processing;
    public void Cancel() => OrderStatus = OrderStatus.Cancelled;
    public void Deliver() => OrderStatus = OrderStatus.Delivered;
}