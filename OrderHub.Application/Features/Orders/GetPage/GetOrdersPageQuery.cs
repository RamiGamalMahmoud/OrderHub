using OrderHub.Application.Authorization;
using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Features.Orders.GetPage;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public record GetOrdersPageQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string SearchTerm = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int? PaymentMethodId = null,
        OrderStatus? OrderStatus = null,
        bool ArchivedOnly = false) : IRequest<PagedResult<OrderListItem>>;
