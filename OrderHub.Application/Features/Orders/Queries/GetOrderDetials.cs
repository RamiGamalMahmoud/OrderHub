using MediatR;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Orders.Queries;

public static class GetOrderDetials
{
    public record Query(int Id) : IRequest<Order>;

    public record Order(
        Header Header,
        Client Client,
        Delivery Delivery,
        IEnumerable<Item> Items,
        Payment Payment);

    public record Header(
        int Id,
        string OrderNumber,
        DateTime CreatedAt,
        string OrderStatus);

    public record Client(
        string Name,
        string PhoneNumber,
        string Address);

    public record Delivery(
        string DeliveryMethod,
        string DeliverymanName,
        string ShippingCarrierName);

    public record Item(
        int ProductId,
        string ProductName,
        decimal UnitPrice,
        int Quantity,
        decimal Total);

    public record Payment(
        string PaymentMethod,
        decimal ProductsTotal,
        decimal ShippingCost,
        decimal Discount,
        decimal GrandTotal);
}
