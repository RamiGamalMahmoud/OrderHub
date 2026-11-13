using MediatR;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Rebuild.GetRebuildHistory;

internal sealed class GetRebuildHistoryQueryHandler(IInventoryRebuildRepository rebuilds)
    : IRequestHandler<GetRebuildHistoryQuery, Result<IReadOnlyList<RebuildRunRow>>>
{
    public async Task<Result<IReadOnlyList<RebuildRunRow>>> Handle(
        GetRebuildHistoryQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RebuildRunRow> rows = await rebuilds.GetHistoryAsync(
            request.Take,
            cancellationToken);
        return Result<IReadOnlyList<RebuildRunRow>>.Success(rows);
    }
}
