using MediatR;
using OrderHub.Application.Features.Inventory.Rebuild.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Rebuild.GetRebuildLog;

internal sealed class GetRebuildLogQueryHandler(IInventoryRebuildRepository rebuilds)
    : IRequestHandler<GetRebuildLogQuery, Result<RebuildLogDetails>>
{
    public async Task<Result<RebuildLogDetails>> Handle(
        GetRebuildLogQuery request,
        CancellationToken cancellationToken)
    {
        if (request.LogId <= 0)
            return Result<RebuildLogDetails>.Failure("سجل إعادة البناء غير صالح.", "NotFound");

        RebuildLogDetails details = await rebuilds.GetDetailsAsync(request.LogId, cancellationToken);
        return details is null
            ? Result<RebuildLogDetails>.Failure("سجل إعادة البناء غير موجود.", "NotFound")
            : Result<RebuildLogDetails>.Success(details);
    }
}
