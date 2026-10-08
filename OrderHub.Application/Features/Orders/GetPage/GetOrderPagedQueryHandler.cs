using MediatR;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Orders.Contracts;

namespace OrderHub.Application.Features.Orders.GetPage;

internal sealed class GetOrderPagedQueryHandler(IOrderReader reader) : IRequestHandler<GetOrdersPageQuery, PagedResult<OrderListItem>>
{
    public Task<PagedResult<OrderListItem>> Handle(GetOrdersPageQuery request, CancellationToken cancellationToken) =>
        reader.GetPageAsync(request.PageNumber, request.PageSize, request.SearchTerm, request.FromDate, request.ToDate, request.PaymentMethodId, request.OrderStatus, cancellationToken, request.ArchivedOnly);
}
