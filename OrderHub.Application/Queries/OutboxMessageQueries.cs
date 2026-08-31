using MediatR;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Queries;

public static class OutboxMessageQueries
{
    public record GetOutboxMessagesQuery() : IRequest<IEnumerable<OutboxMessageListItem>>;
    public sealed record OutboxMessageListItem(
        int Id,
        OutboxMessageStatus Status,
        string OrderNumber,
        string RecipientName,
        RecipientType RecipientType,
        string Text,
        string PhoneNumber,
        DateTime CreatedAt,
        DateTime? LastAttemptAt,
        IReadOnlyList<Attachment> Attachments,
        IReadOnlyList<string> Notes);

    public record Attachment(string OriginalName, string StoredName);
}
