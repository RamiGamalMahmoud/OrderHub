using OrderHub.Domain.Enums;
using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;

namespace OrderHub.Domain.Models;

public class Order : ModelBase
{
    private readonly List<OrderItem> _orderItems = [];
    private Order() { }

    public int ClientId { get; private set; }
    public Client Client { get; private set; }

    public Money Subtotal { get; private set; }
    public Money Tax { get; private set; }
    public Money ShippingCost { get; private set; }
    public Money Discount { get; private set; }
    public Money Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public Address BillingAddress { get; private set; }
}
