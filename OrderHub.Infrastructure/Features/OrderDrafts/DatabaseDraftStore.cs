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

namespace OrderHub.Infrastructure.Features.OrderDrafts;
internal class DatabaseDraftStore : IDraftStore
{
    private readonly AppDbContextFactory _dbContextFactory;
    private readonly ILogger<DatabaseDraftStore> _logger;

    public DatabaseDraftStore(AppDbContextFactory appDbContextFactory, ILogger<DatabaseDraftStore> logger = null)
    {
        _dbContextFactory = appDbContextFactory;
        _logger = logger ?? NullLogger<DatabaseDraftStore>.Instance;
    }

    public async Task DeleteAsync(Guid id, int userId)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        DraftRecord draft = await dbContext.DraftRecords
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if(draft is not null)
        {
            dbContext.DraftRecords.Remove(draft);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<OrderDraftSummary>> GetUserDraftsAsync(int userId)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();

        IReadOnlyList<DraftRecord> draftRecords = await dbContext.DraftRecords
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Where(d => d.UserId == userId)
            .ToListAsync();

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

    public async Task<Draft> GetAsync(Guid id, int userId)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        DraftRecord draftRecord = await dbContext.DraftRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId);

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

    public async Task SaveAsync(Draft draft)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        DraftRecord draftRecord = await dbContext.DraftRecords
            .SingleOrDefaultAsync(orderDraft => orderDraft.Id == draft.Id);

        if (draftRecord is not null && draftRecord.UserId != draft.UserId)
            throw new UnauthorizedAccessException("The draft is not available to this user.");

        string data = JsonSerializer.Serialize(draft);

        if (draftRecord is null)
        {
            DraftRecord newDraftRecord = new DraftRecord()
            {
                Id = draft.Id,
                Data = data,
                LastModified = DateTime.UtcNow,
                UserId = draft.UserId
            };

            dbContext.DraftRecords.Add(newDraftRecord);
        }

        else
        {
            draftRecord.LastModified = DateTime.UtcNow;
            draftRecord.Data = data;
        }

        await dbContext.SaveChangesAsync();
    }

    //public async Task SaveAsync(Draft draft, int userId)
    //{
    //    using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
    //    DraftRecord draftRecord = await dbContext.DraftRecords
    //        .SingleOrDefaultAsync(orderDraft => orderDraft.Id == draft.Id && orderDraft.UserId == userId);

    //    string data = JsonSerializer.Serialize(draftRecord.Data);

    //    if (draftRecord is null)
    //    {
    //        DraftRecord newDraftRecord = new DraftRecord()
    //        {
    //            Id = draft.Id,
    //            Data = data,
    //            LastModified = DateTime.UtcNow,
    //            UserId = userId
    //        };

    //        dbContext.DraftRecords.Add(newDraftRecord);
    //    }

    //    else
    //    {
    //        draftRecord.LastModified = DateTime.UtcNow;
    //        draftRecord.Data = data;
    //    }

    //    await dbContext.SaveChangesAsync();
    //}
}
