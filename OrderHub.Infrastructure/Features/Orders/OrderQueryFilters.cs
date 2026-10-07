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
        // Callers supply UTC instants; the end bound is exclusive. Browser-local
        // day boundaries are converted before this query, without truncating UTC.
        DateTime? fromDateUtc = fromDate?.ToUniversalTime();
        DateTime? toDateUtc = toDate?.ToUniversalTime();

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
