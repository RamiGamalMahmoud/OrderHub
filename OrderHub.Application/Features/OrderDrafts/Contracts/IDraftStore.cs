using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public interface IDraftStore
{
    Task SaveAsync(Draft draft);
    Task<Draft> GetAsync(Guid id);
    Task<IReadOnlyList<OrderDraftSummary>> GetAllAsync();
    Task DeleteAsync(Guid id);
}