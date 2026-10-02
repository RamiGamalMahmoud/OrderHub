using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.OrderDrafts;

public sealed class DraftService(
    IDraftStore store,
    ICurrentUser currentUser) : IDraftService
{
    public async Task SaveAsync(
        DraftContext context,
        OrderDraftData data)
    {
        int userId = await currentUser.GetUserIdAsync()
            ?? throw new UnauthorizedAccessException("Authentication is required for draft operations.");
        Draft draft = new(
            context.Id,
            userId,
            DateTime.UtcNow,
            data);

        await store.SaveAsync(draft);
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
