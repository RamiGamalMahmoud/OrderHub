using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Queries;
using OrderHub.Infrastructure.Extensions;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.QueryHandlers.OutboxMessageQuerieHandlers;

internal sealed class GetOutboxMessagesQueryHandler(
    AppDbContextFactory appDbContextFactory)
    : IRequestHandler<
        OutboxMessageQueries.GetOutboxMessagesQuery,
        PagedResult<OutboxMessageQueries.OutboxMessageListItem>>
{
    public async Task<PagedResult<OutboxMessageQueries.OutboxMessageListItem>> Handle(
        OutboxMessageQueries.GetOutboxMessagesQuery request,
        CancellationToken cancellationToken)
    {
        using AppDbContext dbContext =
            appDbContextFactory.CreateDbContext();

        var query = dbContext
            .OutboxMessages
            .AsNoTracking()
            .ApplyFilter(
                request.SearchTerm,
                request.Status,
                request.FromDate,
                request.ToDate)
            .OrderByDescending(message => message.CreatedAt)
            .Select(message =>
                new OutboxMessageQueries.OutboxMessageListItem(
                    message.Id,
                    message.Status,
                    message.Order.OrderNumber,
                    message.Recipient.Id,
                    message.Recipient.Name,
                    message.RecipientType,
                    message.Text,
                    message.Recipient.PhoneNumber,
                    message.CreatedAt,
                    message.LastAttemptAt,

                    message.Attachments
                        .Select(attachment =>
                            new OutboxMessageQueries.Attachment(
                                attachment.OriginalFileName,
                                attachment.StoredFileName,
                                Path.GetExtension(
                                    attachment.StoredFileName),
                                attachment.FileSize))
                        .ToList(),

                    message.Notes));

        return await query.ToPagedResultAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}