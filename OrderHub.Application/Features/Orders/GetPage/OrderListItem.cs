using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Features.Orders.GetPage;

public record OrderListItem(
    int Id,
    string OrderNumber,
    string ClientOrderTitle,
    string ClientName,
    string ClientPhoneNumber,
    int ItemsCount,
    decimal Total,
    OrderStatus OrderStatus,
    EnumItem<OrderStatus> EnumItem,
    OrderPaymentMethodItem PaymentMethod,
    DateTime CreatedAt);
