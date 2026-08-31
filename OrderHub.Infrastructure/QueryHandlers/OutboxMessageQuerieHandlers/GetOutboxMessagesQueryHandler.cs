using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.QueryHandlers.OutboxMessageQuerieHandlers;

internal class GetOutboxMessagesQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<OutboxMessageQueries.GetOutboxMessagesQuery, IEnumerable<OutboxMessageQueries.OutboxMessageListItem>>
{
    public async Task<IEnumerable<OutboxMessageQueries.OutboxMessageListItem>> Handle(OutboxMessageQueries.GetOutboxMessagesQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();
        return await appDbContext
            .OutboxMessages
            .AsNoTracking()
            .Select(m => new OutboxMessageQueries.OutboxMessageListItem(
                m.Id,
                m.Status,
                m.Order.OrderNumber,
                m.Recipient.Name,
                m.RecipientType,
                m.Text,
                m.Recipient.PhoneNumber,
                m.CreatedAt,
                m.LastAttemptAt,
                m.Attachments.Select(x => new OutboxMessageQueries.Attachment(x.OriginalFileName, x.StoredFileName)).ToList(),
                m.Notes))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
