using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Orders.Queries;

internal class GetOrderPagedQueryHandler(AppDbContextFactory dbContextFactory) : IRequestHandler<GetOrderPaged.Query, PagedResult<GetOrderPaged.Order>>
{
    public async Task<PagedResult<GetOrderPaged.Order>> Handle(GetOrderPaged.Query request, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = dbContextFactory.CreateDbContext();

        var query = dbContext.Orders
            .AsNoTracking()
            .ApplyFilter(request.SearchTerm, request.FromDate, request.ToDate, request.PaymentMethodId, request.OrderStatus)
            .OrderByDescending(o => o.CreatedAt)
            .Select(order => new GetOrderPaged.Order(
                order.Id,
                order.OrderNumber,
                "",
                order.Client.Name.Value,
                order.Client.Phone.Number.FullNumber,
                order.OrderItems.Count,
                order.OrderItems.Sum(oi => oi.UnitPrice.Value * oi.Quantity),
                order.OrderStatus,

                new EnumItem<OrderStatus>(
                    order.OrderStatus,
                    order.OrderStatus.GetDescription()),

                order.PaymentMethod == null ? null : new GetOrderPaged.PaymentMethod(
                    order.PaymentMethod.Id,
                    order.PaymentMethod.DisplayName,
                    order.PaymentMethod.Description,
                    order.PaymentMethod.IsActive),

                order.CreatedAt
            ));

        return await query.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}

internal static class OrderQueryFilters
{
    public static IQueryable<Domain.Models.Order> ApplyFilter(
        this IQueryable<Domain.Models.Order> query,
        string searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        int? paymentMethodId,
        OrderStatus? orderStatus)
    {
        DateTime? fromDateUtc = fromDate?.Date.ToUniversalTime();
        DateTime? toDateUtc = toDate?.Date.AddDays(1).ToUniversalTime();

        return query.Where(order =>
            (string.IsNullOrWhiteSpace(searchTerm)
                || order.Client.Name.Value.Contains(searchTerm)
                || order.OrderNumber.Contains(searchTerm))
            && (!fromDateUtc.HasValue || order.CreatedAt >= fromDateUtc.Value)
            && (!toDateUtc.HasValue || order.CreatedAt < toDateUtc.Value)
            && (!paymentMethodId.HasValue || order.PaymentMethodId == paymentMethodId.Value)
            && (!orderStatus.HasValue || order.OrderStatus == orderStatus.Value));
    }
}