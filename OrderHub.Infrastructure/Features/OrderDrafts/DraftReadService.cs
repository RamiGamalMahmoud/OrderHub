using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OrderHub.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.OrderDrafts;
internal class DraftReadService : IDraftReadService
{
    private readonly AppDbContextFactory _dbContextFactory;
    private readonly ILogger<DraftReadService> _logger;

    public DraftReadService(AppDbContextFactory appDbContextFactory, ILogger<DraftReadService> logger = null)
    {
        _dbContextFactory = appDbContextFactory;
        _logger = logger ?? NullLogger<DraftReadService>.Instance;
    }

    public async Task<IReadOnlyList<OrderDraftSummary>> GetUserDraftsAsync(int userId, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();

        IReadOnlyList<DraftRecord> draftRecords = await dbContext.DraftRecords
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);

        var summaries = new List<OrderDraftSummary>();
        foreach (var draftRecord in draftRecords)
        {
            try
            {
                Draft draft = JsonSerializer.Deserialize<Draft>(draftRecord.Data);
                if (draft?.Data?.Items is null)
                    throw new JsonException("Draft data is incomplete or invalid.");
                summaries.Add(new OrderDraftSummary(
                    draftRecord.Id, draft.Data.ClientName, draft.Data.Items.Count, draftRecord.LastModified));
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Skipping malformed draft {DraftId} in summaries.", draftRecord.Id);
            }
        }
        return summaries;
    }

    public async Task<Draft> GetOwnedAsync(Guid id, int userId, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        DraftRecord draftRecord = await dbContext.DraftRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (draftRecord is null || draftRecord.Data is null)
            return null;

        var draft = JsonSerializer.Deserialize<Draft>(draftRecord.Data);
        return draft is null ? null : draft with
        {
            Id = draftRecord.Id,
            UserId = draftRecord.UserId,
            LastModified = draftRecord.LastModified
        };
    }

}
