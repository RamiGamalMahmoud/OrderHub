using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public interface IDraftStore
{
    Task SaveAsync(Draft draft);

    Task<Draft> GetAsync(Guid id);

    Task<IReadOnlyList<OrderDraftSummary>> GetAllAsync();

    Task<IReadOnlyList<OrderDraftSummary>> GetUserDraftsAsync(int userId);

    Task DeleteAsync(Guid id);
}