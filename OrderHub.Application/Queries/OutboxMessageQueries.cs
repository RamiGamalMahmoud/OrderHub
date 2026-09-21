using MediatR;
using OrderHub.Application.Common;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderHub.Application.Queries;

public static class OutboxMessageQueries
{
    public sealed record GetOutboxMessagesQuery(
        int PageNumber,
        int PageSize,
        string SearchTerm = null,
        OutboxMessageStatus? Status = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null)
        : IRequest<PagedResult<OutboxMessageListItem>>;

    public sealed record OutboxMessageListItem(
        int Id,
        OutboxMessageStatus Status,
        string OrderNumber,
        int RecipientId,
        string RecipientName,
        RecipientType RecipientType,
        string Text,
        string PhoneNumber,
        DateTime CreatedAt,
        DateTime? LastAttemptAt,
        IReadOnlyList<Attachment> Attachments,
        IReadOnlyList<string> Notes);

    public sealed record Attachment(
        string OriginalName,
        string StoredName,
        string Extension,
        double Size);
}