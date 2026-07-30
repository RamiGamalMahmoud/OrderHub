using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace OrderHub.Tests.Data;

public class OrderBuilder
{
    private int _clientId = 1;
    private Client _client = new Client("Default Client", "123 Street", new City("Cairo"), "0123456789", "+20");
    private string _orderNumber = Guid.NewGuid().ToString().Substring(0, 8);
    private List<OrderItem> _items = new List<OrderItem>();
    private DeliveryMethod? _deliveryMethod = null;
    private OrderStatus _orderStatus = OrderStatus.Pending;
    private ShippingCarrier _shippingCarrier = null;
    private Deliveryman _deliveryman = null;
    private PaymentMethod _paymentMethod = null;

    public OrderBuilder WithClient(Client client)
    {
        _client = client;
        return this;
    }

    public OrderBuilder WithClientId(int clientId)
    {
        _clientId = clientId;
        return this;
    }

    public OrderBuilder WithOrderNumber(string orderNumber)
    {
        _orderNumber = orderNumber;
        return this;
    }

    public OrderBuilder AddOrderItem(OrderItem item)
    {
        _items.Add(item);
        return this;
    }

    public OrderBuilder WithDeliveryMethod(DeliveryMethod method)
    {
        _deliveryMethod = method;
        return this;
    }

    public OrderBuilder WithOrderStatus(OrderStatus status)
    {
        _orderStatus = status;
        return this;
    }

    public OrderBuilder WithShippingCarrier(ShippingCarrier carrier)
    {
        _shippingCarrier = carrier;
        return this;
    }

    public OrderBuilder WithDeliveryman(Deliveryman deliveryman)
    {
        _deliveryman = deliveryman;
        return this;
    }

    public OrderBuilder WithPaymentMethod(PaymentMethod paymentMethod)
    {
        _paymentMethod = paymentMethod;
        return this;
    }

    //public Order Build()
    //{
    //    var order = new Order(_clientId, _orderNumber);
    //    foreach (var item in _items)
    //    {
    //        order.AddOrderItem(item);
    //    }

    //    order.ChangeOrderStatus(_orderStatus);
    //    order.ShippingCarrier = _shippingCarrier;
    //    order.Deliveryman = _deliveryman;
    //    order.DeliveryMethod = _deliveryMethod;
    //    order.PaymentMethod = _paymentMethod;
    //    return order;
    //}
}