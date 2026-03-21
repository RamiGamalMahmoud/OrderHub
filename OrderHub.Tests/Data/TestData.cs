using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;

public static class TestData
{
    public static Client CreateClient(string name = "Default Client")
    {
        return new Client(
            name,
            "123 Main Street",
            new City($"{name} City"),
            "01012345678",
            "+20"
        );
    }

    public static Deliveryman CreateDeliveryman(string name = "Delivery Guy")
    {
        return new Deliveryman(
            name,
            new City($"{name} City"),
            "01112345678"
        );
    }

    public static ShippingCarrier CreateShippingCarrier(string name = "FastShip", decimal shippingCost = 20)
    {
        var phone = Phone.Create("0123456789", "+20").Value;
        var address = Address.Create("Street 1", new City($"{name} City")).Value;

        return new ShippingCarrier(name, shippingCost, phone, address);
    }

    public static Supplier CreateSupplier(string name = "Supplier A")
    {
        var phone = Phone.Create("01298765432", "+20").Value;
        var address = Address.Create("Supplier Street", new City($"{name} City")).Value;
        var open = new TimeOnly(9, 0);
        var close = new TimeOnly(18, 0);

        return new Supplier(name, open, close, address, phone);
    }

    public static OrderItem CreateOrderItem(int productId, string productName, int orderId, decimal unitPrice, int quantity, Supplier supplier)
    {
        return new OrderItem(
            productId,
            productName,
            orderId,
            unitPrice,
            quantity,
            supplier.Name.Value,
            supplier.Id
        );
    }

    public static Order CreateOrder(Client client = null, Deliveryman deliveryman = null, ShippingCarrier carrier = null)
    {
        client ??= CreateClient();
        deliveryman ??= CreateDeliveryman();
        carrier ??= CreateShippingCarrier();

        var order = new Order(clientId: client.Id, orderNumber: Guid.NewGuid().ToString().Substring(0, 8))
        {
            Deliveryman = deliveryman,
            ShippingCarrier = carrier,
        };

        // Add sample order items
        var supplier1 = CreateSupplier("Supplier 1");
        var supplier2 = CreateSupplier("Supplier 2");

        order.AddOrderItem(CreateOrderItem(1, "Product A", order.Id, 50, 2, supplier1));
        order.AddOrderItem(CreateOrderItem(2, "Product B", order.Id, 100, 1, supplier2));

        return order;
    }

    public static List<Order> CreateOrders(int count = 3)
    {
        var orders = new List<Order>();
        for (int i = 0; i < count; i++)
        {
            orders.Add(CreateOrder());
        }
        return orders;
    }
}
