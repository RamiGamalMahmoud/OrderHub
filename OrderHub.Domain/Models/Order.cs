using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public class Order : ModelBase
{
    private readonly List<OrderItem> _orderItems = [];
    private Order() { }

    public int ClientId { get; private set; }

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public Money Total => new Money(_orderItems.Sum(i => i.SubTotal.Value));
    public int OrderStatusId { get; private set; }
    public OrderStatus OrderStatus { get; private set; }

    public void AddOrderItem(OrderItem orderItem) => _orderItems.Add(orderItem);

    public void RemoveOrderItem(OrderItem orderItem)
    {
        OrderItem item = _orderItems.FirstOrDefault(i => i == orderItem);
        if (item is not null)
        {
            _orderItems.Remove(item);
        }
        _orderItems.Remove(orderItem);
    }

    public void ChangeOrderStatus(OrderStatus orderStatus) => OrderStatus = orderStatus;
}
