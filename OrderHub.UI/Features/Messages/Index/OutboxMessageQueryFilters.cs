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

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();

            query = query.Where(message =>
                message.Order.OrderNumber.Contains(term) ||
                message.Recipient.Name.Contains(term) ||
                message.Recipient.PhoneNumber.Contains(term) ||
                message.Text.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(message =>
                message.Status == status.Value);
        }

        if (fromDateUtc.HasValue)
        {
            query = query.Where(message =>
                message.CreatedAt >= fromDateUtc.Value);
        }

        if (toDateUtc.HasValue)
        {
            query = query.Where(message =>
                message.CreatedAt < toDateUtc.Value);
        }

        return query;
    }
}