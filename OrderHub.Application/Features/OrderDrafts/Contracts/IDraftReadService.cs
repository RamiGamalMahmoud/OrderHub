using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public interface IDraftReadService
{
    Task<Draft> GetOwnedAsync(Guid id, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDraftSummary>> GetUserDraftsAsync(int userId, CancellationToken cancellationToken = default);
}
