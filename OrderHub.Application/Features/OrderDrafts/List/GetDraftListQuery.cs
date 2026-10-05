using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;

namespace OrderHub.Application.Features.OrderDrafts.List;

public sealed record GetDraftListQuery : IRequest<IReadOnlyList<OrderDraftSummary>>;

internal sealed class GetDraftListQueryHandler(ICurrentUser currentUser, IDraftReadService reads)
    : IRequestHandler<GetDraftListQuery, IReadOnlyList<OrderDraftSummary>>
{
    public async Task<IReadOnlyList<OrderDraftSummary>> Handle(GetDraftListQuery request, CancellationToken cancellationToken) =>
        await reads.GetUserDraftsAsync(await DraftUser.RequireAsync(currentUser, cancellationToken), cancellationToken);
}
