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
    private Order() { }

    public int ClientId { get; private set; }
    public Client Client { get; private set; }

    public Order(int clientId, string orderNumber)
    {
        ClientId = clientId;
        OrderNumber = orderNumber;
    }

    public string OrderNumber { get; private set; }

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public IReadOnlyCollection<OrderDeliveryStep> DeliverySteps => _deliverySteps.AsReadOnly();
    public IReadOnlyCollection<OrderEntitySequence> EntitySequences => _entitySequences.AsReadOnly();

    private readonly List<OutboxMessage> _outboxMessages = [];
    public IReadOnlyCollection<OutboxMessage> OutboxMessages => _outboxMessages.AsReadOnly();

    public Money Total => new Money(_orderItems.Sum(i => i.SubTotal.Value));
    public DeliveryMethod? DeliveryMethod { get; set; }
    public OrderStatus OrderStatus { get; private set; }
    public int? ShippingCarrierId { get; set; }
    public ShippingCarrier ShippingCarrier { get; set; }
    public int? DeliverymanId { get; set; }
    public Deliveryman Deliveryman { get; set; }
    public int? PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public void AddOrderItem(OrderItem orderItem) => _orderItems.Add(orderItem);

    public void AddDeliveryStep(OrderDeliveryStep deliveryStep) => _deliverySteps.Add(deliveryStep);

    public void AddEntitySequence(OrderEntitySequence entitySequence) => _entitySequences.Add(entitySequence);

    public void ChangeClient(int clientId) => ClientId = clientId;

    public void RemoveOrderItem(OrderItem orderItem)
    {
        OrderItem item = _orderItems.FirstOrDefault(i => i == orderItem);
        if (item is not null)
        {
            _orderItems.Remove(item);
        }
        _orderItems.Remove(orderItem);
    }

    public void ClearOrderItems() => _orderItems.Clear();

    public void ClearDeliverySteps() => _deliverySteps.Clear();

    public void RemoveEntitySequence(OrderEntitySequence entitySequence)
    {
        OrderEntitySequence sequence = _entitySequences.FirstOrDefault(current => current == entitySequence);
        if (sequence is not null)
        {
            _entitySequences.Remove(sequence);
        }
    }

    public void ChangeOrderStatus(OrderStatus orderStatus) => OrderStatus = orderStatus;

    public string GetDisplayTitle(RecipientType recipientType, int? entityId = null)
    {
        int resolvedEntityId = entityId ?? recipientType switch
        {
            RecipientType.Client => ClientId,
            RecipientType.Deliveryman => DeliverymanId ?? 0,
            _ => 0
        };

        if (resolvedEntityId <= 0)
        {
            return OrderNumber;
        }

        return _entitySequences
            .FirstOrDefault(sequence =>
                sequence.RecipientType == recipientType
                && sequence.EntityId == resolvedEntityId)
            ?.DisplayTitle
            ?? OrderNumber;
    }
}
