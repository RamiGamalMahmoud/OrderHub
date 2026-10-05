using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Infrastructure.Models;
using System.Text.Json;

namespace OrderHub.Infrastructure.Features.OrderDrafts;

internal sealed class DraftRepository(AppDbContext context) : IDraftRepository
{
    public async Task StageSaveAsync(Draft draft, CancellationToken cancellationToken = default)
    {
        var record = await context.DraftRecords.SingleOrDefaultAsync(row => row.Id == draft.Id, cancellationToken);
        if (record is not null && record.UserId != draft.UserId)
            throw new UnauthorizedAccessException("The draft is not available to this user.");
        var json = JsonSerializer.Serialize(draft);
        if (record is null)
            context.DraftRecords.Add(new DraftRecord
            {
                Id = draft.Id, UserId = draft.UserId, Data = json, LastModified = DateTime.UtcNow
            });
        else
        {
            record.Data = json;
            record.LastModified = DateTime.UtcNow;
        }
    }
    public async Task StageDeleteAsync(Guid id, int userId, CancellationToken cancellationToken = default)
    {
        var draft = await context.DraftRecords.SingleOrDefaultAsync(
            record => record.Id == id && record.UserId == userId, cancellationToken);
        if (draft is not null)
            context.DraftRecords.Remove(draft);
    }
}
