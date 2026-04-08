using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.Queries.OrderQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Orders;

internal class GetOrdersSummaryQueryHandler(AppDbContextFactory appDbContextFactory)
    : IRequestHandler<GetOrdersSummaryQuery, OrderSummaryDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<OrderSummaryDto> Handle(GetOrdersSummaryQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        IQueryable<Domain.Models.Order> filteredOrders = appDbContext.Orders
            .AsNoTracking()
            .ApplyFilter(request.SearchTerm, request.FromDate, request.ToDate, request.PaymentMethodId, request.OrderStatus);

        var statusCounts = await filteredOrders
            .GroupBy(order => order.OrderStatus)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var paymentMethodCounts = await filteredOrders
            .GroupBy(order => new
            {
                order.PaymentMethodId,
                PaymentMethodName = order.PaymentMethod != null ? order.PaymentMethod.DisplayName : "بدون وسيلة دفع"
            })
            .Select(group => new
            {
                group.Key.PaymentMethodId,
                group.Key.PaymentMethodName,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var statusSummaries = Enum
            .GetValues(typeof(OrderStatus))
            .Cast<OrderStatus>()
            .Select(status => new OrderStatusSummaryDto(
                status,
                new EnumItem<OrderStatus>(status, status.GetDescription()),
                statusCounts.FirstOrDefault(item => item.Status == status)?.Count ?? 0))
            .ToArray();

        var paymentMethods = await appDbContext.PaymentMethods
            .AsNoTracking()
            .OrderBy(paymentMethod => paymentMethod.DisplayName)
            .Select(paymentMethod => new
            {
                paymentMethod.Id,
                paymentMethod.DisplayName
            })
            .ToListAsync(cancellationToken);

        var paymentMethodSummaries = paymentMethods
            .Select(paymentMethod => new OrderPaymentMethodSummaryDto(
                paymentMethod.Id,
                paymentMethod.DisplayName,
                paymentMethodCounts.FirstOrDefault(item => item.PaymentMethodId == paymentMethod.Id)?.Count ?? 0))
            .Concat(new[]
            {
                new OrderPaymentMethodSummaryDto(
                    null,
                    "بدون وسيلة دفع",
                    paymentMethodCounts.FirstOrDefault(item => item.PaymentMethodId == null)?.Count ?? 0)
            })
            .ToArray();

        return new OrderSummaryDto(statusSummaries, paymentMethodSummaries);
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
        return query.Where(order =>
            (string.IsNullOrWhiteSpace(searchTerm)
                || order.Client.Name.Value.Contains(searchTerm)
                || order.OrderNumber.Contains(searchTerm)
                || order.EntitySequences.Any(sequence =>
                    sequence.RecipientType == RecipientType.Client
                    && sequence.EntityId == order.ClientId
                    && sequence.DisplayTitle.Contains(searchTerm)))
            && (!fromDate.HasValue || order.CreatedAt >= fromDate.Value.Date)
            && (!toDate.HasValue || order.CreatedAt < toDate.Value.Date.AddDays(1))
            && (!paymentMethodId.HasValue || order.PaymentMethodId == paymentMethodId.Value)
            && (!orderStatus.HasValue || order.OrderStatus == orderStatus.Value));
    }
}
