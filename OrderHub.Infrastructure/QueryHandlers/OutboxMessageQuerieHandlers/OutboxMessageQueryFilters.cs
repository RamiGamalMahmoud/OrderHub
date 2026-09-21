using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;

namespace OrderHub.Infrastructure.QueryHandlers.OutboxMessageQuerieHandlers;

internal static class OutboxMessageQueryFilters
{
    public static IQueryable<OutboxMessage> ApplyFilter(
        this IQueryable<OutboxMessage> query,
        string searchTerm,
        OutboxMessageStatus? status,
        DateTime? fromDate,
        DateTime? toDate)
    {
        DateTime? fromDateUtc =
            fromDate?.Date.ToUniversalTime();

        DateTime? toDateUtc =
            toDate?.Date
                .AddDays(1)
                .ToUniversalTime();

        return query.Where(message =>
            (
                string.IsNullOrWhiteSpace(searchTerm)
                || message.Order.OrderNumber.Contains(searchTerm)
                || message.Recipient.Name.Contains(searchTerm)
                || message.Recipient.PhoneNumber.Contains(searchTerm)
                || message.Text.Contains(searchTerm)
            )
            && (
                !status.HasValue
                || message.Status == status.Value
            )
            && (
                !fromDateUtc.HasValue
                || message.CreatedAt >= fromDateUtc.Value
            )
            && (
                !toDateUtc.HasValue
                || message.CreatedAt < toDateUtc.Value
            ));
    }
}