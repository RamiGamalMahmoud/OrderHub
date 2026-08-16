using OrderHub.Application.Features.OrderDrafts.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.OrderDrafts;

internal sealed class DraftService(IDraftStore store) : IDraftService
{
    public Task SaveAsync(
        DraftContext context,
        OrderDraftData data)
    {
        Draft draft = new(
            context.Id,
            DateTime.UtcNow,
            data);

        return store.SaveAsync(draft);
    }

    public Task<Draft> GetAsync(DraftContext context)
    {
        return store.GetAsync(context.Id);
    }

    public Task DeleteAsync(DraftContext context)
    {
        return store.DeleteAsync(context.Id);
    }

    public async Task<IReadOnlyList<OrderDraftSummary>> GetAllAsync()
    {
        return await store.GetAllAsync();
    }
}