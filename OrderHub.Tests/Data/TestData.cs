using OrderHub.Application.Features.Setup.Properties.Create;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Stores;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    public static Order CreateOrder(Client client = null, Deliveryman deliveryman = null, ShippingCarrier carrier = null)
    {
        client ??= CreateClient();
        deliveryman ??= CreateDeliveryman();
        carrier ??= CreateShippingCarrier();

        var order = new Order(clientId: client.Id, orderNumber: Guid.NewGuid().ToString().Substring(0, 8));
        order.ChangeDeliveryman(deliveryman.Id);
        order.ChangeShippingCarrier(carrier.Id);

        // Add sample order items
        var supplier1 = CreateSupplier("Supplier 1");
        var supplier2 = CreateSupplier("Supplier 2");

        order.AddOrderItem(1, "Product A", order.Id, 50, supplier1.Name.Value, 2, []);
        order.AddOrderItem(2, "Product B", order.Id, 100, supplier2.Name.Value, 1, []);

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

    internal static async Task<Property> CreateProperty(AppDbContextFactory dbContextFactory)
    {
        var property = Property.Create(
            "Color",
            OrderHub.Domain.Enums.PropertyType.List,
            "Description",
            ["Red", "Blue"]);

        using AppDbContext appDbContext = dbContextFactory.CreateDbContext();
        appDbContext.Properties.Add(property);
        await appDbContext.SaveChangesAsync();

        return property;
    }
}
