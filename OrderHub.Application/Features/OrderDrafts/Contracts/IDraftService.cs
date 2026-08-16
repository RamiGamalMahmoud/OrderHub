using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public interface IDraftService
{
    Task SaveAsync(DraftContext context, OrderDraftData data);

    Task<Draft> GetAsync(DraftContext context);

    Task<IReadOnlyList<OrderDraftSummary>> GetAllAsync();

    Task DeleteAsync(DraftContext context);
}