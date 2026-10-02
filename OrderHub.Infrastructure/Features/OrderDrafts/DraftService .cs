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
        int userId = await RequireUserAsync();
        Draft draft = new(
            context.Id,
            userId,
            DateTime.UtcNow,
            data);

        await store.SaveAsync(draft);
    }

    public async Task<Draft> GetAsync(DraftContext context)
    {
        return await store.GetAsync(context.Id, await RequireUserAsync());
    }

    public async Task DeleteAsync(DraftContext context)
    {
        await store.DeleteAsync(context.Id, await RequireUserAsync());
    }

    public async Task<IReadOnlyList<OrderDraftSummary>> GetAllAsync()
    {
        return await store.GetUserDraftsAsync(await RequireUserAsync());
    }

    private async Task<int> RequireUserAsync()
    {
        var userId = await currentUser.GetUserIdAsync();
        return userId is > 0 ? userId.Value
            : throw new UnauthorizedAccessException("Authentication is required for draft operations.");
    }
}
