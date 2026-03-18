using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Queries.OutboxMessageQueries;

namespace OrderHub.Infrastructure.QueryHandlers.OutboxMessageQuerieHandlers;

internal class GetOutboxMessagesQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetOutboxMessagesQuery, IEnumerable<OutboxMessage>>
{
    public async Task<IEnumerable<OutboxMessage>> Handle(GetOutboxMessagesQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();
        return await appDbContext
            .OutboxMessages
            .Include(m => m.Order)
            .Include(m => m.Recipient)
            .ToListAsync();
    }
}
