using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Features.Orders.Queries;

public static class GetOrderPaged
{
    public record Query(
        int PageNumber = 1,
        int PageSize = 20,
        string SearchTerm = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int? PaymentMethodId = null,
        OrderStatus? OrderStatus = null) : IRequest<PagedResult<Order>>;

    public record Order(
    int Id,
    string OrderNumber,
    string ClientOrderTitle,
    string ClientName,
    string ClientPhoneNumber,
    int ItemsCount,
    decimal Total,
    OrderStatus OrderStatus,
    EnumItem<OrderStatus> EnumItem,
    PaymentMethod PaymentMethod,
    DateTime CreatedAt,
    bool HasClientRecipient,
    bool HasSupplierRecipient,
    bool HasShippingCarrierRecipient,
    bool HasDeliverymanRecipient,
    bool IsClientMessageSent,
    bool IsSupplierMessageSent,
    bool IsShippingCarrierMessageSent,
    bool IsDeliverymanMessageSent);

    public record PaymentMethod(int Id, string DisplayName, string Description, bool IsActive);
}
