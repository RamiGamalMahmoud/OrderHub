using OrderHub.Domain.Enums;
using System;
using System.Linq;

namespace OrderHub.Infrastructure.Features.Orders;

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
