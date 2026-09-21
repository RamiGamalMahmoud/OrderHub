using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.OrderDrafts;

internal sealed class DraftService(
    IDraftStore store,
    IApplicationContext applicationContext) : IDraftService
{
    private readonly IApplicationContext _applicationContext = applicationContext;

    public Task SaveAsync(
        DraftContext context,
        OrderDraftData data)
    {
        Draft draft = new(
            context.Id,
            _applicationContext.User.Id,
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
        return await store.GetUserDraftsAsync(_applicationContext.User.Id);
    }
}