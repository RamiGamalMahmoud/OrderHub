using MediatR;
using OrderHub.Application.Common;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.Application.Queries;

public static class OrderQueries
{
    public record GetOrdersQuery : IRequest<IEnumerable<OrderListDto>>;
    public record GetOrdersPagedQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string SearchTerm = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int? PaymentMethodId = null,
        OrderStatus? OrderStatus = null) : IRequest<PagedResult<OrderListDto>>;
    public record GetOrdersSummaryQuery(
        string SearchTerm = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int? PaymentMethodId = null,
        OrderStatus? OrderStatus = null) : IRequest<OrderSummaryDto>;
    public record GetClientOrdersQuery(string ClientName) : IRequest<IEnumerable<OrderListDto>>;
    public record GetOrderForEditQuery(int OrderId) : IRequest<OrderEditDto>;

    public record SearchAttributeNamesQuery(string SearchTerm) : IRequest<IEnumerable<AttributeNameDto>>;
}
