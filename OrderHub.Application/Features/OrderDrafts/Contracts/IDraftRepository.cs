using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.OrderDrafts.Contracts;

public interface IDraftRepository
{
    // Stage consumption in the caller's scoped UnitOfWork; never commit independently.
    Task StageDeleteAsync(Guid id, int userId, CancellationToken cancellationToken = default);
}
