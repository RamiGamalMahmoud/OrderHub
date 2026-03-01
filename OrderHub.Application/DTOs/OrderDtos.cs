using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.Application.DTOs;

public static class OrderDtos
{
    public record OrderListDto(
        int Id,
        string OrderNumber,
        OrderStatusInfoDto OrderStatusInfoDto,
        string ClientName,
        string ClientPhoneNumber,
        int ItemsCount,
        decimal Total,
        DateTime CreatedAt);

    public record OrderCreateDto(
        int ClientId,
        int OrderStatusId,
        DeliveryMethod DeliveryMethod,
        int? DeliveryManId,
        int? ShippingCarrierId,
        IEnumerable<OrderItemDtos.OrderItemDto> OrderItems);
}

public static class OrderItemDtos
{
    public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
}
