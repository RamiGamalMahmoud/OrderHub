using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.OrderDrafts.Contracts;

namespace OrderHub.Infrastructure.Features.OrderDrafts;

internal sealed class DraftRepository(AppDbContext context) : IDraftRepository
{
    public async Task StageDeleteAsync(Guid id, int userId, CancellationToken cancellationToken = default)
    {
        var draft = await context.DraftRecords.SingleOrDefaultAsync(
            record => record.Id == id && record.UserId == userId, cancellationToken);
        if (draft is not null)
            context.DraftRecords.Remove(draft);
    }
}
