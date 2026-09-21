using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.OrderDrafts.Contracts;
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

    public DatabaseDraftStore(AppDbContextFactory appDbContextFactory)
    {
        _dbContextFactory = appDbContextFactory;
    }

    public async Task DeleteAsync(Guid id)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        DraftRecord draft = await dbContext.DraftRecords
            .SingleOrDefaultAsync(x => x.Id == id);

        if(draft is not null)
        {
            dbContext.DraftRecords.Remove(draft);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<OrderDraftSummary>> GetAllAsync()
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();

        IReadOnlyList<DraftRecord> draftRecords = await dbContext.DraftRecords
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return draftRecords.Select(draftRecord =>
        {
            Draft draft = JsonSerializer.Deserialize<Draft>(draftRecord.Data);
            return new OrderDraftSummary(
                draftRecord.Id,
                draft.Data.ClientName,
                draft.Data.Items.Count,
                draft.LastModified);
        })
            .ToList();
    }

    public async Task<IReadOnlyList<OrderDraftSummary>> GetUserDraftsAsync(int userId)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();

        IReadOnlyList<DraftRecord> draftRecords = await dbContext.DraftRecords
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Where(d => d.UserId == userId)
            .ToListAsync();

        return draftRecords.Select(draftRecord =>
        {
            Draft draft = JsonSerializer.Deserialize<Draft>(draftRecord.Data);
            return new OrderDraftSummary(
                draftRecord.Id,
                draft.Data.ClientName,
                draft.Data.Items.Count,
                draft.LastModified);
        })
            .ToList();
    }

    public async Task<Draft> GetAsync(Guid id)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        DraftRecord draftRecord = await dbContext.DraftRecords
            .SingleOrDefaultAsync(x => x.Id == id);

        if (draftRecord is null || draftRecord.Data is null)
            return null;

        return JsonSerializer.Deserialize<Draft>(draftRecord.Data);
    }

    public async Task SaveAsync(Draft draft)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        DraftRecord draftRecord = await dbContext.DraftRecords
            .SingleOrDefaultAsync(orderDraft => orderDraft.Id == draft.Id && orderDraft.UserId == draft.UserId);

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
